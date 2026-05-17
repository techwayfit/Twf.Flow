namespace Twf.Flow.Core;

/// <summary>
/// The result of executing a single node.
/// Contains the output data, execution status, timing, and error information.
/// Supports fluent chaining: result.OnSuccess(...).OnFailure(...)
/// </summary>
public sealed class NodeResult
{
    // ─── Core Properties ─────────────────────────────────────────────────────

    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;

    /// <summary>The output data to pass to the next node.</summary>
    public WorkflowData Data { get; private init; } = new();

    /// <summary>Node name that produced this result.</summary>
    public string NodeName { get; private init; } = string.Empty;

    /// <summary>Final execution status.</summary>
    public NodeStatus Status { get; private init; }

    /// <summary>Error message if the node failed.</summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>Original exception if the node threw.</summary>
    public Exception? Exception { get; private init; }

    /// <summary>How long this node took to execute.</summary>
    public TimeSpan Duration { get; private init; }

    /// <summary>UTC timestamp when execution started.</summary>
    public DateTime StartedAt { get; private init; }

    /// <summary>UTC timestamp when execution completed.</summary>
    public DateTime CompletedAt { get; private init; }

    /// <summary>Arbitrary metadata the node wants to surface (e.g., token counts, HTTP status).</summary>
    public Dictionary<string, object> Metadata { get; private init; } = new();

    /// <summary>Optional log messages emitted by the node during execution.</summary>
    public List<string> Logs { get; private init; } = new();

    // ─── Factory Methods ─────────────────────────────────────────────────────

    public static NodeResult Success(
        string nodeName,
        WorkflowData data,
        TimeSpan duration,
        DateTime startedAt,
        Dictionary<string, object>? metadata = null,
        List<string>? logs = null) => new()
    {
        IsSuccess = true,
        NodeName = nodeName,
        Status = NodeStatus.Success,
        Data = data,
        Duration = duration,
        StartedAt = startedAt,
        CompletedAt = startedAt + duration,
        Metadata = metadata ?? new(),
        Logs = logs ?? new()
    };

    public static NodeResult Failure(
        string nodeName,
        WorkflowData data,
        string errorMessage,
        Exception? exception,
        TimeSpan duration,
        DateTime startedAt,
        NodeStatus status = NodeStatus.Failed) => new()
    {
        IsSuccess = false,
        NodeName = nodeName,
        Status = status,
        Data = data,
        ErrorMessage = errorMessage,
        Exception = exception,
        Duration = duration,
        StartedAt = startedAt,
        CompletedAt = startedAt + duration,
        Metadata = new(),
        Logs = new()
    };

    public static NodeResult Skipped(string nodeName, WorkflowData data) => new()
    {
        IsSuccess = true,
        NodeName = nodeName,
        Status = NodeStatus.Skipped,
        Data = data,
        Duration = TimeSpan.Zero,
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow
    };

    // ─── Fluent Callbacks ────────────────────────────────────────────────────

    /// <summary>Execute an action if this result is successful.</summary>
    public NodeResult OnSuccess(Action<WorkflowData> action)
    {
        if (IsSuccess) action(Data);
        return this;
    }

    /// <summary>Execute an action if this result is a failure.</summary>
    public NodeResult OnFailure(Action<string, Exception?> action)
    {
        if (IsFailure) action(ErrorMessage ?? "Unknown error", Exception);
        return this;
    }

    /// <summary>Transform data if successful; otherwise pass failure through.</summary>
    public NodeResult Map(Func<WorkflowData, WorkflowData> transform)
    {
        if (!IsSuccess) return this;
        return Success(NodeName, transform(Data), Duration, StartedAt, Metadata, Logs);
    }

    // ─── Display ─────────────────────────────────────────────────────────────

    public override string ToString() =>
        IsSuccess
            ? $"✅ [{NodeName}] {Status} in {Duration.TotalMilliseconds:F0}ms"
            : $"❌ [{NodeName}] {Status}: {ErrorMessage} (in {Duration.TotalMilliseconds:F0}ms)";
}
