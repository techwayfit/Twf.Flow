using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Control;

// ═══════════════════════════════════════════════════════════════════════════════
// DelayNode — Insert a delay between nodes
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Introduces a configurable delay in the pipeline.
/// Useful for rate limiting, waiting for async processes, or debugging.
/// </summary>
public sealed class DelayNode : BaseNode
{
    public override string Name => $"Delay:{_delay.TotalMilliseconds}ms";
    public override string Category => "Control";
    public override string Description =>
        $"Waits for {_delay.TotalMilliseconds}ms before passing data to next node";

    /// <inheritdoc/>

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>

    private readonly TimeSpan _delay;
    private readonly string? _reason;

    public DelayNode(TimeSpan delay, string? reason = null)
    {
        _delay = delay;
        _reason = reason;
    }

    /// <summary>Dictionary constructor for dynamic instantiation. Parameter: durationMs (double).</summary>
    public DelayNode(Dictionary<string, object?> parameters)
        : this(
            TimeSpan.FromMilliseconds(NodeParameters.GetDouble(parameters, "durationMs", 1000)),
            NodeParameters.GetString(parameters, "reason"))
    { }

    public static DelayNode Milliseconds(int ms, string? reason = null) =>
        new(TimeSpan.FromMilliseconds(ms), reason);

    public static DelayNode Seconds(int seconds, string? reason = null) =>
        new(TimeSpan.FromSeconds(seconds), reason);

    public static DelayNode RateLimitDelay(int requestsPerMinute) =>
        Milliseconds((int)(60000.0 / requestsPerMinute), "Rate limit");

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        nodeCtx.Log($"Waiting {_delay.TotalMilliseconds}ms" +
                    (_reason is not null ? $" ({_reason})" : ""));

        await Task.Delay(_delay, context.CancellationToken).ConfigureAwait(false);
        return input;
    }
}