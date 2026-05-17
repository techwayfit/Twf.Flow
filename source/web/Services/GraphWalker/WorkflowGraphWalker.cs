using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Services.Execution;
using Twf.Flow.Web.Services.NodeFactory;

namespace Twf.Flow.Web.Services.GraphWalker;

/// <summary>
/// Walks a workflow graph by traversing nodes and connections.
/// Handles structural nodes (Start, End, Error), control flow (Branch, Loop, SubWorkflow),
/// and regular node execution with proper routing.
/// </summary>
public class WorkflowGraphWalker : IWorkflowGraphWalker
{
    private readonly INodeFactory _nodeFactory;
    private readonly INodeExecutor _nodeExecutor;
    private readonly ILogger<WorkflowGraphWalker> _logger;

    private const int MaxSteps = 500; // Guard against infinite loops

    public WorkflowGraphWalker(
        INodeFactory nodeFactory,
        INodeExecutor nodeExecutor,
        ILogger<WorkflowGraphWalker> logger)
    {
        _nodeFactory = nodeFactory;
        _nodeExecutor = nodeExecutor;
        _logger = logger;
    }

    public async Task<WalkResult> WalkAsync(WalkConfiguration config)
    {
        var currentId = config.StartNodeId;
        var data = config.Data;
        var steps = 0;

        while (steps++ < MaxSteps)
        {
            // Check for cancellation
            config.Context.CancellationToken.ThrowIfCancellationRequested();

            if (!config.NodeMap.TryGetValue(currentId, out var nodeDef))
            {
                _logger.LogWarning("Node {NodeId} not found in workflow — stopping", currentId);
                break;
            }

            _logger.LogDebug("→ [{NodeType}] {NodeName}", nodeDef.Type, nodeDef.Name);

            // Handle structural nodes
            if (nodeDef.Type is "StartNode")
            {
                var nextId = await HandleStartNodeAsync(nodeDef, currentId, config);
                if (!nextId.HasValue) break;
                currentId = nextId.Value;
                continue;
            }

            if (nodeDef.Type is "EndNode")
            {
                await HandleEndNodeAsync(nodeDef, config);
                return WalkResult.CreateSuccess(data);
            }

            if (nodeDef.Type is "ErrorNode")
            {
                var nextId = await HandleErrorNodeAsync(nodeDef, currentId, config);
                if (!nextId.HasValue)
                    return WalkResult.CreateSuccess(data);
                currentId = nextId.Value;
                continue;
            }

            // Handle SubWorkflowNode
            if (nodeDef.Type is "SubWorkflowNode")
            {
                var (subData, subSuccess, subError, subFailedNode, subNextId) = await HandleSubWorkflowNodeAsync(
               nodeDef,
                currentId,
              data,
            config);

                if (!subSuccess)
                {
                    return WalkResult.CreateFailure(subData, subError!, subFailedNode);
                }

                data = subData;
                if (!subNextId.HasValue) break;
                currentId = subNextId.Value;
                continue;
            }

            // Handle LoopNode
            if (nodeDef.Type is "LoopNode")
            {
                var (loopData, loopSuccess, loopError, loopFailedNode, loopNextId) = await HandleLoopNodeAsync(
                   nodeDef,
                    currentId,
                      data,
                     config);

                if (!loopSuccess)
                {
                    return WalkResult.CreateFailure(loopData, loopError!, loopFailedNode);
                }

                data = loopData;
                if (!loopNextId.HasValue) break;
                currentId = loopNextId.Value;
                continue;
            }

            // Handle regular nodes
            var (nodeData, nodeSuccess, nodeError, nodePort, nodeNextId) = await HandleRegularNodeAsync(
             nodeDef,
            currentId,
                data,
            config);

            if (!nodeSuccess)
            {
                // Try error routing
                var errorRouteId = TryRouteToError(
                  currentId,
                   nodeDef,
                 nodeError!,
                   config);

                if (errorRouteId.HasValue)
                {
                    data = data.Clone()
                    .Set("error_message", nodeError)
                     .Set("failed_node", nodeDef.Name);
                    currentId = errorRouteId.Value;
                    continue;
                }

                return WalkResult.CreateFailure(nodeData, nodeError!, nodeDef.Name);
            }

            data = nodeData;
            if (!nodeNextId.HasValue)
            {
                _logger.LogDebug(
                   "No outgoing connection from '{NodeName}' — stopping",
                   nodeDef.Name);
                break;
            }

            currentId = nodeNextId.Value;
        }

        if (steps >= MaxSteps)
        {
            return WalkResult.CreateFailure(
           data,
          "Workflow exceeded maximum step limit (possible infinite loop)");
        }

        return WalkResult.CreateSuccess(data);
    }

    // ─── Structural Node Handlers ─────────────────────────────────────────────

    private async Task<Guid?> HandleStartNodeAsync(
           NodeDefinition nodeDef,
     Guid currentId,
           WalkConfiguration config)
    {
        await using var scope = await NodeStepScope.StartAsync(
  nodeDef, [], false, false, config.OnStep);
        scope.Complete([]);

        return config.Routing.TryGetValue((currentId, "output"), out var nextId)
         ? nextId
      : null;
    }

    private async Task HandleEndNodeAsync(
    NodeDefinition nodeDef,
      WalkConfiguration config)
    {
        await using var scope = await NodeStepScope.StartAsync(
          nodeDef, [], false, false, config.OnStep);
        scope.Complete([]);
    }

    private async Task<Guid?> HandleErrorNodeAsync(
    NodeDefinition nodeDef,
   Guid currentId,
      WalkConfiguration config)
    {
        return config.Routing.TryGetValue((currentId, "output"), out var nextId)
            ? nextId
       : null;
    }

    // ─── SubWorkflow Handler ──────────────────────────────────────────────────

    private async Task<(WorkflowData data, bool success, string? error, string? failedNode, Guid? nextId)> HandleSubWorkflowNodeAsync(
     NodeDefinition nodeDef,
   Guid currentId,
     WorkflowData data,
          WalkConfiguration config)
    {
        var subIdStr = NodeParameters.GetString(nodeDef.Parameters, "subWorkflowId");
        if (!Guid.TryParse(subIdStr, out var subId))
        {
            // No valid sub-workflow ID — pass through
            var fallbackId = config.Routing.TryGetValue((currentId, "output"), out var fId)
             ? fId
            : (Guid?)null;
            return (data, true, null, null, fallbackId);
        }

        var childDef = config.WorkflowDefinition.SubWorkflows
     .FirstOrDefault(sw => sw.Id == subId);

        if (childDef == null)
        {
            _logger.LogWarning(
                "SubWorkflow {SubId} not found in workflow '{WorkflowName}'",
              subId,
                  config.WorkflowDefinition.Name);

            var fallbackId = config.Routing.TryGetValue((currentId, "output"), out var fId)
              ? fId
               : (Guid?)null;
            return (data, true, null, null, fallbackId);
        }

        var childStart = childDef.Nodes.FirstOrDefault(n => n.Type == "StartNode");
        if (childStart == null)
        {
            _logger.LogWarning(
         "SubWorkflow {SubId} has no Start node",
             subId);

            var fallbackId = config.Routing.TryGetValue((currentId, "output"), out var fId)
        ? fId
         : (Guid?)null;
            return (data, true, null, null, fallbackId);
        }

        // Build transient workflow definition for the child
        var childWrapDef = new WorkflowDefinition
        {
            Id = childDef.Id,
            Name = childDef.Name,
            Nodes = childDef.Nodes,
            Connections = childDef.Connections,
            Variables = childDef.Variables,
            ErrorNodeId = childDef.ErrorNodeId,
            SubWorkflows = config.WorkflowDefinition.SubWorkflows
        };

        var childNodeMap = childDef.Nodes.ToDictionary(n => n.Id);
        var childRouting = BuildRouting(childDef.Connections);

        var childConfig = new WalkConfiguration
        {
            StartNodeId = childStart.Id,
            Data = data.Clone(),
            Context = config.Context,
            NodeMap = childNodeMap,
            Routing = childRouting,
            WorkflowDefinition = childWrapDef,
            OnStep = config.OnStep
        };

        var childResult = await WalkAsync(childConfig);

        var selectedPort = childResult.Success ? "success" : "error";
        var nextId = config.Routing.TryGetValue((currentId, selectedPort), out var nId)
          ? nId
       : (Guid?)null;

        if (!childResult.Success && !nextId.HasValue)
        {
            return (childResult.Data, false, childResult.ErrorMessage, childResult.FailedNodeName, null);
        }

        return (childResult.Data, true, null, null, nextId);
    }

    // ─── Loop Handler ─────────────────────────────────────────────────────────

    private async Task<(WorkflowData data, bool success, string? error, string? failedNode, Guid? nextId)> HandleLoopNodeAsync(
        NodeDefinition nodeDef,
 Guid currentId,
 WorkflowData data,
        WalkConfiguration config)
    {
        var itemsKeyRaw = NodeParameters.GetString(nodeDef.Parameters, "itemsKey") ?? "items";
        var itemsKeyMatch = System.Text.RegularExpressions.Regex.Match(
   itemsKeyRaw, @"^\{\{([\w.]+)\}\}$");
        var itemsKey = itemsKeyMatch.Success ? itemsKeyMatch.Groups[1].Value : itemsKeyRaw;
        var outputKey = NodeParameters.GetString(nodeDef.Parameters, "outputKey") ?? "results";
        var loopItemKey = NodeParameters.GetString(nodeDef.Parameters, "loopItemKey") ?? "__item__";
        var maxIter = NodeParameters.GetInt(nodeDef.Parameters, "maxIterations");

        var loopInput = new Dictionary<string, object?> { ["itemsKey"] = itemsKey };
        await using var loopScope = await NodeStepScope.StartAsync(
   nodeDef, loopInput, true, true, config.OnStep);

        var rawItems = (data.Get<IEnumerable<object>>(itemsKey)
       ?? data.Get<object>(itemsKey) as IEnumerable<object>
          ?? (data.Get<object>(itemsKey) is System.Collections.IEnumerable ie
      ? ie.Cast<object>() : null))?.ToList();

        if (rawItems is null)
        {
            _logger.LogWarning(
              "LoopNode '{NodeName}': key '{ItemsKey}' not found or empty — skipping loop",
       nodeDef.Name,
                 itemsKey);

            loopScope.Fail($"Key '{itemsKey}' not found or is not a collection");

            var skipId = config.Routing.TryGetValue((currentId, "output"), out var sId)
        ? sId
         : (Guid?)null;

            return (data, true, null, null, skipId);
        }

        if (maxIter > 0 && rawItems.Count > maxIter)
            rawItems = rawItems.Take(maxIter).ToList();

        var loopOutputs = new List<WorkflowData>(rawItems.Count);
        var hasBodyPort = config.Routing.TryGetValue((currentId, "body"), out var bodyEntryId);

        for (var loopIdx = 0; loopIdx < rawItems.Count; loopIdx++)
        {
            config.Context.CancellationToken.ThrowIfCancellationRequested();

            var itemData = data
            .Set(loopItemKey, rawItems[loopIdx])
         .Set("__loop_index__", loopIdx);

            if (hasBodyPort)
            {
                // Signal loop iteration start
                await config.OnStep(new NodeStepEvent
                {
                    EventType = "loop_iteration_start",
                    NodeId = nodeDef.Id,
                    NodeName = nodeDef.Name,
                    NodeType = nodeDef.Type
                });

                var bodyConfig = new WalkConfiguration
                {
                    StartNodeId = bodyEntryId,
                    Data = itemData,
                    Context = config.Context,
                    NodeMap = config.NodeMap,
                    Routing = config.Routing,
                    WorkflowDefinition = config.WorkflowDefinition,
                    OnStep = config.OnStep
                };

                var iterResult = await WalkAsync(bodyConfig);

                if (!iterResult.Success)
                {
                    loopScope.Fail($"Iteration {loopIdx} failed: {iterResult.ErrorMessage}");
                    return (data, false,
                   $"LoopNode '{nodeDef.Name}' iteration {loopIdx} failed: {iterResult.ErrorMessage}",
                         iterResult.FailedNodeName ?? nodeDef.Name,
                      null);
                }

                data = iterResult.Data;
                loopOutputs.Add(iterResult.Data);
            }
            else
            {
                loopOutputs.Add(itemData);
            }
        }

        data = data.Clone()
               .Set(outputKey, loopOutputs)
          .Set("loop_iteration_count", rawItems.Count);

        if (!string.IsNullOrEmpty(nodeDef.NodeId))
        {
            data.Set($"{nodeDef.NodeId}.{outputKey}", loopOutputs);
            data.Set($"{nodeDef.NodeId}.loop_iteration_count", rawItems.Count);
        }

        loopScope.Complete(new()
        {
            [outputKey] = loopOutputs,
            ["loop_iteration_count"] = rawItems.Count
        });

        var nextId = config.Routing.TryGetValue((currentId, "output"), out var nId)
 ? nId
       : (Guid?)null;

        return (data, true, null, null, nextId);
    }

    // ─── Regular Node Handler ─────────────────────────────────────────────────

    private async Task<(WorkflowData data, bool success, string? error, string? port, Guid? nextId)> HandleRegularNodeAsync(
  NodeDefinition nodeDef,
        Guid currentId,
      WorkflowData data,
      WalkConfiguration config)
    {
        var node = _nodeFactory.CreateNode(nodeDef, data);
        if (node is null)
        {
            _logger.LogWarning("Unknown node type '{NodeType}' — skipping", nodeDef.Type);
            var skipId = config.Routing.TryGetValue((currentId, "output"), out var sId)
                ? sId
           : (Guid?)null;
            return (data, true, null, "output", skipId);
        }

        var opts = BuildNodeOptions(nodeDef.ExecutionOptions);

        var dataInKeys = node.DataIn.Select(p => p.Key).ToHashSet();
        var dataOutKeys = node.DataOut.Select(p => p.Key).ToHashSet();
        var hasDataIn = dataInKeys.Count > 0;
        var hasDataOut = dataOutKeys.Count > 0;

        var inputSnapshot = Snapshot(data);
        var filteredInput = hasDataIn
           ? inputSnapshot.Where(kv => dataInKeys.Contains(kv.Key))
        .ToDictionary(kv => kv.Key, kv => kv.Value)
            : new Dictionary<string, object?>();

        await using var scope = await NodeStepScope.StartAsync(
        nodeDef, filteredInput, hasDataIn, hasDataOut, config.OnStep);

        // Validate required inputs
        var missingRequired = node.DataIn
          .Where(p => p.Required && !data.Keys.Contains(p.Key))
        .Select(p => p.Key)
          .ToList();

        if (missingRequired.Count > 0)
        {
            var missing = string.Join(", ", missingRequired);
            var msg = $"Missing required input(s): {missing}";
            _logger.LogError(
             "Node '{NodeName}' ({NodeId}): {Message}",
                 nodeDef.Name,
                   nodeDef.NodeId,
                    msg);
            scope.Fail(msg);
            return (data, false, $"Node '{nodeDef.Name}' ({nodeDef.NodeId}): {msg}", null, null);
        }

        try
        {
            var resultData = await _nodeExecutor.ExecuteAsync(node, data, config.Context, opts);

            // Write scoped outputs
            if (!string.IsNullOrEmpty(nodeDef.NodeId))
            {
                foreach (var port in node.DataOut)
                {
                    if (resultData.Keys.Contains(port.Key))
                    {
                        resultData.Set(
                   $"{nodeDef.NodeId}.{port.Key}",
                       resultData.Get<object>(port.Key));
                    }
                }
            }

            var activatedPort = GetActivatedPort(nodeDef, resultData);
            var fullOutputSnapshot = Snapshot(resultData);
            var filteredOutput = hasDataOut
             ? fullOutputSnapshot.Where(kv => dataOutKeys.Contains(kv.Key))
           .ToDictionary(kv => kv.Key, kv => kv.Value)
             : new Dictionary<string, object?>();

            scope.Complete(filteredOutput);

            var nextId = config.Routing.TryGetValue((currentId, activatedPort), out var nId)
        ? nId
     : (Guid?)null;

            return (resultData, true, null, activatedPort, nextId);
        }
        catch (Exception ex) when (opts.ContinueOnError)
        {
            _logger.LogWarning(ex, "Node '{NodeName}' failed but ContinueOnError=true", nodeDef.Name);
            scope.Fail(ex.Message);

            var fallbackData = opts.FallbackData ?? data;
            var nextId = config.Routing.TryGetValue((currentId, "output"), out var nId)
                ? nId
          : (Guid?)null;

            return (fallbackData, true, null, "output", nextId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Node '{NodeName}' failed", nodeDef.Name);
            scope.Fail(ex.Message);
            return (data, false, ex.Message, null, null);
        }
    }

    // ─── Helper Methods ───────────────────────────────────────────────────────

    private Guid? TryRouteToError(
     Guid currentId,
        NodeDefinition nodeDef,
 string errorMessage,
        WalkConfiguration config)
    {
        // Try node's own error port first
        if (config.Routing.TryGetValue((currentId, "error"), out var nodeErrorId))
        {
            return nodeErrorId;
        }

        // Try workflow-level error node
        if (config.WorkflowDefinition.ErrorNodeId.HasValue &&
config.NodeMap.ContainsKey(config.WorkflowDefinition.ErrorNodeId.Value) &&
      config.Routing.TryGetValue(
     (config.WorkflowDefinition.ErrorNodeId.Value, "output"),
            out var errorHandlerId))
        {
            return errorHandlerId;
        }

        return null;
    }

    private static string GetActivatedPort(NodeDefinition nodeDef, WorkflowData resultData)
    {
        return nodeDef.Type switch
        {
            "BranchNode" => resultData.GetString("branch_selected_port") ?? "default",
            "ErrorRouteNode" => resultData.GetString("error_route") ?? "success",
            _ => "output"
        };
    }

    private static Dictionary<string, object?> Snapshot(WorkflowData data)
        => data.Keys.ToDictionary(k => k, k => data.Get<object?>(k));

    private static Dictionary<(Guid nodeId, string port), Guid> BuildRouting(
        List<ConnectionDefinition> connections)
    {
        var table = new Dictionary<(Guid, string), Guid>();
        foreach (var c in connections)
        {
            var key = (c.SourceNodeId, c.SourcePort);
            if (!table.ContainsKey(key))
                table[key] = c.TargetNodeId;
        }
        return table;
    }

    private static NodeOptions BuildNodeOptions(NodeExecutionOptions? opts)
    {
        if (opts is null) return NodeOptions.Default;

        var result = NodeOptions.Default;

        if (opts.MaxRetries > 0)
            result = result.AndRetry(opts.MaxRetries, TimeSpan.FromMilliseconds(opts.RetryDelayMs));

        if (opts.TimeoutMs.HasValue)
            result = result.AndTimeout(TimeSpan.FromMilliseconds(opts.TimeoutMs.Value));

        if (opts.ContinueOnError)
        {
            WorkflowData? fallback = null;
            if (opts.FallbackData?.Count > 0)
            {
                fallback = new WorkflowData();
                foreach (var (k, v) in opts.FallbackData)
                    fallback.Set(k, v);
            }
            result = result.AndContinueOnError(fallback);
        }

        return result;
    }
}
