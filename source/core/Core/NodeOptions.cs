namespace Twf.Flow.Core;

/// <summary>
/// Per-node execution configuration. Controls retry behavior, timeouts,
/// conditional skipping, and error handling strategy.
/// Applied at the workflow builder level: .AddNode(node, NodeOptions.WithRetry(3))
/// </summary>
public sealed record NodeOptions
{
    // ─── Defaults ────────────────────────────────────────────────────────────

    public static readonly NodeOptions Default = new();

    // ─── Retry ───────────────────────────────────────────────────────────────

    /// <summary>Maximum retry attempts after the first failure. Default: 0 (no retry).</summary>
    public int MaxRetries { get; init; } = 0;

    /// <summary>Base delay between retries. Uses exponential backoff: delay * 2^attempt.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Only retry on these specific exception types. Empty = retry all.</summary>
    public Type[] RetryOnExceptions { get; init; } = Array.Empty<Type>();

    // ─── Timeout ─────────────────────────────────────────────────────────────

    /// <summary>Maximum execution time for this node. Null = no timeout.</summary>
    public TimeSpan? Timeout { get; init; }

    // ─── Condition ───────────────────────────────────────────────────────────

    /// <summary>
    /// Skip this node if the condition returns false.
    /// Useful for conditional steps: skip if feature flag is off, etc.
    /// </summary>
    public Func<WorkflowData, bool>? RunCondition { get; init; }

    // ─── Error Handling ───────────────────────────────────────────────────────

    /// <summary>
    /// If true, a failure in this node will not stop the workflow.
    /// The pipeline continues with the last successful data.
    /// </summary>
    public bool ContinueOnError { get; init; } = false;

    /// <summary>
    /// Optional fallback data to use if this node fails.
    /// Combined with ContinueOnError to provide a default value.
    /// </summary>
    public WorkflowData? FallbackData { get; init; }

    // ─── Fluent Builder ───────────────────────────────────────────────────────

    public static NodeOptions WithRetry(int maxRetries, TimeSpan? delay = null) => new()
    {
        MaxRetries = maxRetries,
        RetryDelay = delay ?? TimeSpan.FromSeconds(1)
    };

    public static NodeOptions WithTimeout(TimeSpan timeout) => new()
    {
        Timeout = timeout
    };

    public static NodeOptions WithCondition(Func<WorkflowData, bool> condition) => new()
    {
        RunCondition = condition
    };

    public NodeOptions AndContinueOnError(WorkflowData? fallback = null) => this with
    {
        ContinueOnError = true,
        FallbackData = fallback
    };

    public NodeOptions AndTimeout(TimeSpan timeout) => this with { Timeout = timeout };

    public NodeOptions AndRetry(int retries, TimeSpan? delay = null) => this with
    {
        MaxRetries = retries,
        RetryDelay = delay ?? RetryDelay
    };

    public override string ToString() =>
        $"NodeOptions[Retry={MaxRetries}, Timeout={Timeout?.TotalSeconds}s, ContinueOnError={ContinueOnError}]";
}
