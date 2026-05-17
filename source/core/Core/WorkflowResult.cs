using Twf.Flow.Tracking;

namespace Twf.Flow.Core;

/// <summary>
/// The final result of a complete workflow execution.
/// Contains the output data, full execution report, and per-node results.
/// </summary>
public sealed class WorkflowResult
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;

    public string WorkflowName { get; private init; } = string.Empty;
    public string RunId { get; private init; } = string.Empty;

    /// <summary>Final data output from the last successful node.</summary>
    public WorkflowData Data { get; private init; } = new();

    /// <summary>Total workflow execution duration.</summary>
    public TimeSpan TotalDuration { get; private init; }

    public DateTime StartedAt { get; private init; }
    public DateTime CompletedAt { get; private init; }

    /// <summary>All per-node results, in execution order.</summary>
    public IReadOnlyList<NodeResult> NodeResults { get; private init; } = Array.Empty<NodeResult>();

    /// <summary>Name of the node that caused failure, if any.</summary>
    public string? FailedNodeName { get; private init; }

    public string? ErrorMessage { get; private init; }

    public Exception? Exception { get; private init; }

    /// <summary>Full execution report with timing breakdown.</summary>
    public WorkflowReport Report { get; private init; } = new();

    // ─── Factory ─────────────────────────────────────────────────────────────

    public static WorkflowResult Success(
        string workflowName,
        string runId,
        WorkflowData data,
        List<NodeResult> nodeResults,
        DateTime startedAt,
        WorkflowReport report) => new()
    {
        IsSuccess = true,
        WorkflowName = workflowName,
        RunId = runId,
        Data = data,
        NodeResults = nodeResults,
        StartedAt = startedAt,
        CompletedAt = DateTime.UtcNow,
        TotalDuration = DateTime.UtcNow - startedAt,
        Report = report
    };

    public static WorkflowResult Failure(
        string workflowName,
        string runId,
        WorkflowData data,
        List<NodeResult> nodeResults,
        string failedNode,
        string errorMessage,
        Exception? exception,
        DateTime startedAt,
        WorkflowReport report) => new()
    {
        IsSuccess = false,
        WorkflowName = workflowName,
        RunId = runId,
        Data = data,
        NodeResults = nodeResults,
        FailedNodeName = failedNode,
        ErrorMessage = errorMessage,
        Exception = exception,
        StartedAt = startedAt,
        CompletedAt = DateTime.UtcNow,
        TotalDuration = DateTime.UtcNow - startedAt,
        Report = report
    };

    // ─── Fluent Callbacks ────────────────────────────────────────────────────

    public WorkflowResult OnSuccess(Action<WorkflowData> action)
    {
        if (IsSuccess) action(Data);
        return this;
    }

    public WorkflowResult OnFailure(Action<string, Exception?> action)
    {
        if (IsFailure) action(ErrorMessage ?? "Unknown error", Exception);
        return this;
    }

    // ─── Display ─────────────────────────────────────────────────────────────

    public string Summary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════╗");
        sb.AppendLine($"  Workflow: {WorkflowName} | Run: {RunId}");
        sb.AppendLine($"  Status:   {(IsSuccess ? "✅ SUCCESS" : "❌ FAILED")}");
        sb.AppendLine($"  Duration: {TotalDuration.TotalMilliseconds:F0}ms");
        sb.AppendLine($"  Nodes:    {NodeResults.Count} executed");
        if (!IsSuccess)
            sb.AppendLine($"  Error:    [{FailedNodeName}] {ErrorMessage}");
        sb.AppendLine($"╚══════════════════════════════════════════════════╝");
        foreach (var nr in NodeResults)
            sb.AppendLine($"   {nr}");
        return sb.ToString();
    }

    public override string ToString() =>
        $"WorkflowResult[{WorkflowName}/{RunId}] {(IsSuccess ? "✅" : "❌")} in {TotalDuration.TotalMilliseconds:F0}ms";
}
