using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Web.Models;
using Twf.Flow.Web.Services.GraphWalker;
using Twf.Flow.Web.Extensions;

namespace Twf.Flow.Web.Services;

/// <summary>
/// Orchestrates workflow execution by coordinating the graph walker and managing workflow lifecycle.
/// This is now a lightweight orchestrator — the heavy lifting is delegated to specialized services:
///   - INodeFactory: Creates node instances from definitions
///   - IVariableResolver: Resolves {{variable}} templates in parameters
///   - IWorkflowGraphWalker: Traverses the workflow graph and executes nodes
///   - INodeExecutor: Executes individual nodes with retry/timeout support
/// </summary>
public sealed class WorkflowDefinitionRunner
{
    private readonly IWorkflowGraphWalker _graphWalker;
  private readonly ILogger<WorkflowDefinitionRunner> _logger;

  public WorkflowDefinitionRunner(
        IWorkflowGraphWalker graphWalker,
    ILogger<WorkflowDefinitionRunner> logger)
{
_graphWalker = graphWalker;
        _logger = logger;
    }

    /// <summary>
/// Execute the workflow and fire <paramref name="onStep"/> before and after each node.
/// Use this overload when streaming real-time progress to a UI (e.g. Server-Sent Events).
/// </summary>
    public async Task<WorkflowRunResult> RunWithCallbackAsync(
        WorkflowDefinition definition,
        WorkflowData? initialData,
  Func<NodeStepEvent, Task> onStep)
    {
        // Validate workflow has a Start node
        var startNode = definition.Nodes.FirstOrDefault(n => n.Type == "StartNode")
  ?? throw new InvalidOperationException(
    $"Workflow '{definition.Name}' has no Start node. " +
  "Drag a Start node onto the canvas and save before running.");

        // Begin structured logging scope for workflow
        using var workflowScope = _logger.BeginWorkflowScope(
   definition.Id,
        definition.Name,
   definition.Nodes.Count);

      // Prepare workflow context and data
        var context = new WorkflowContext(definition.Name, _logger);
     var data = initialData?.Clone() ?? new WorkflowData();

   // Seed workflow-level variables into both WorkflowContext (code-API compat)
 // and WorkflowData so {{variable}} substitution works in nodes
foreach (var (key, value) in definition.Variables)
        {
    context.SetState(key, value);
  if (!data.Keys.Contains(key)) // initial data takes priority
 data.Set(key, value);
    }

        _logger.LogInformation(
          "▶ Starting workflow execution: {WorkflowName} ({WorkflowId}) with {NodeCount} nodes",
            definition.Name,
          definition.Id,
            definition.Nodes.Count);

        var startTime = DateTime.UtcNow;

     // Build routing table and node map
        var nodeMap = definition.Nodes.ToDictionary(n => n.Id);
        var routing = BuildRouting(definition.Connections);

     // Configure graph walk
      var walkConfig = new WalkConfiguration
    {
   StartNodeId = startNode.Id,
 Data = data,
   Context = context,
   NodeMap = nodeMap,
    Routing = routing,
   WorkflowDefinition = definition,
   OnStep = onStep
   };

    // Execute the workflow graph
   var walkResult = await _graphWalker.WalkAsync(walkConfig).ConfigureAwait(false);

        var duration = DateTime.UtcNow - startTime;

// Map result to WorkflowRunResult
   if (walkResult.Success)
        {
      _logger.LogInformation(
"✅ Workflow execution completed successfully: {WorkflowName} in {DurationMs}ms",
  definition.Name,
                (int)duration.TotalMilliseconds);

   // Log performance metric
    _logger.LogPerformanceMetric(
     "workflow_execution_duration",
     duration.TotalMilliseconds,
 "ms",
       new Dictionary<string, object>
       {
         ["workflow_id"] = definition.Id,
          ["workflow_name"] = definition.Name,
     ["node_count"] = definition.Nodes.Count,
   ["success"] = true
     });

   return WorkflowRunResult.Success(definition.Name, walkResult.Data);
      }
 else
    {
      _logger.LogError(
    "❌ Workflow execution failed: {WorkflowName} at node '{FailedNode}' after {DurationMs}ms: {Error}",
  definition.Name,
        walkResult.FailedNodeName ?? "unknown",
           (int)duration.TotalMilliseconds,
     walkResult.ErrorMessage);

            // Log performance metric for failed execution
   _logger.LogPerformanceMetric(
    "workflow_execution_duration",
     duration.TotalMilliseconds,
  "ms",
            new Dictionary<string, object>
       {
          ["workflow_id"] = definition.Id,
  ["workflow_name"] = definition.Name,
      ["node_count"] = definition.Nodes.Count,
          ["success"] = false,
          ["failed_node"] = walkResult.FailedNodeName ?? "unknown"
        });

return WorkflowRunResult.Failure(
     definition.Name,
          walkResult.Data,
 walkResult.ErrorMessage,
        walkResult.FailedNodeName);
   }
    }

    /// <summary>Execute the workflow without a step callback (single-shot JSON result).</summary>
 public Task<WorkflowRunResult> RunAsync(
      WorkflowDefinition definition,
 WorkflowData? initialData = null)
        => RunWithCallbackAsync(definition, initialData, _ => Task.CompletedTask);

/// <summary>
    /// Builds a routing table from workflow connections for efficient graph traversal.
    /// Maps (sourceNodeId, outputPort) → targetNodeId.
  /// </summary>
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
}
