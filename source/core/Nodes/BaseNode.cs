using Twf.Flow.Core;
using Microsoft.Extensions.Logging;

namespace Twf.Flow.Nodes;

/// <summary>
/// Abstract base class for all Twf.Flow nodes.
/// Automatically handles:
///   - Execution timing (start/end timestamps)
///   - Structured logging with node context
///   - Exception catching and wrapping in NodeResult
///   - Metadata collection
///   - Log collection
/// 
/// Subclass this and implement RunAsync() to create a custom node.
/// </summary>
public abstract class BaseNode : INode
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    public abstract string Name { get; }
    public abstract string Category { get; }
    public virtual string Description => $"{Category}/{Name} node";

    // Optional metadata contracts.
    public virtual string IdPrefix => "node";
    public virtual IReadOnlyList<NodeData> DataIn  => [];
    public virtual IReadOnlyList<NodeData> DataOut => [];

    // ─── Template Method ─────────────────────────────────────────────────────

    /// <summary>
    /// Implement your node logic here. Called by ExecuteAsync after setup.
    /// The NodeContext gives you a scoped logger and metadata/log collectors.
    /// </summary>
    protected abstract Task<WorkflowData> RunAsync(
        WorkflowData input,
        WorkflowContext context,
        NodeExecutionContext nodeCtx);

    // ─── INode.ExecuteAsync ──────────────────────────────────────────────────

    public async Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context)
    {
        var startedAt = DateTime.UtcNow;
        var nodeCtx = new NodeExecutionContext(Name, context.Logger);

        context.Logger.LogInformation("▶ [{Node}] Starting", Name);

        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var output = await RunAsync(data, context, nodeCtx).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startedAt;
            context.Logger.LogInformation(
                "✅ [{Node}] Completed in {DurationMs}ms", Name, duration.TotalMilliseconds);

            return NodeResult.Success(Name, output, duration, startedAt,
                nodeCtx.Metadata, nodeCtx.Logs);
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - startedAt;
            context.Logger.LogWarning("🚫 [{Node}] Cancelled after {DurationMs}ms",
                Name, duration.TotalMilliseconds);
            return NodeResult.Failure(Name, data, "Workflow was cancelled", null,
                duration, startedAt, NodeStatus.Cancelled);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startedAt;
            context.Logger.LogError(ex,
                "❌ [{Node}] Failed after {DurationMs}ms: {Error}",
                Name, duration.TotalMilliseconds, ex.Message);
            return NodeResult.Failure(Name, data, ex.Message, ex, duration, startedAt);
        }
    }

    public override string ToString() => $"{Category}/{Name}";
}

/// <summary>
/// Scoped context for a single node execution.
/// Provides a log collector, metadata collector, and scoped logger.
/// </summary>
public sealed class NodeExecutionContext
{
    public string NodeName { get; }
    public ILogger Logger { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public List<string> Logs { get; } = new();

    public NodeExecutionContext(string nodeName, ILogger logger)
    {
        NodeName = nodeName;
        Logger = logger;
    }

    /// <summary>Emit a log message that will appear in the execution report.</summary>
    public void Log(string message)
    {
        Logs.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        Logger.LogDebug("  [{Node}] {Message}", NodeName, message);
    }

    /// <summary>Record a metadata value surfaced in the execution report.</summary>
    public void SetMetadata(string key, object value) => Metadata[key] = value;
}

/// <summary>
/// Convenience base for nodes that take and return a specific data key.
/// Reduces boilerplate for simple single-key transform nodes.
/// </summary>
public abstract class SimpleTransformNode : BaseNode
{
    protected abstract string InputKey { get; }
    protected abstract string OutputKey { get; }

    protected abstract Task<object?> TransformAsync(object? input, WorkflowContext context);

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var value = input.Get<object>(InputKey);
        var result = await TransformAsync(value, context).ConfigureAwait(false);
        return input.Clone().Set(OutputKey, result);
    }
}
