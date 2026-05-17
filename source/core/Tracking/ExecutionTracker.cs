using Twf.Flow.Core;

namespace Twf.Flow.Tracking;

/// <summary>
/// Records the execution of every node in a workflow run.
/// Provides timing breakdowns, status tracking, and generates reports.
/// </summary>
public sealed class ExecutionTracker
{
    private readonly List<NodeExecutionRecord> _records = new();
    private readonly object _lock = new();

    // ─── Recording ───────────────────────────────────────────────────────────

    public NodeExecutionRecord BeginNode(string nodeName, string nodeCategory)
    {
        var record = new NodeExecutionRecord
        {
            NodeName = nodeName,
            NodeCategory = nodeCategory,
            StartedAt = DateTime.UtcNow,
            Status = NodeStatus.Running
        };

        lock (_lock) { _records.Add(record); }
        return record;
    }

    public void CompleteNode(NodeExecutionRecord record, NodeResult result)
    {
        record.Status = result.Status;
        record.CompletedAt = DateTime.UtcNow;
        record.Duration = (record.CompletedAt - record.StartedAt) ?? TimeSpan.Zero;
        record.ErrorMessage = result.ErrorMessage;
        record.Metadata = result.Metadata;
        record.Logs = result.Logs.ToList();
    }

    // ─── Queries ─────────────────────────────────────────────────────────────

    public IReadOnlyList<NodeExecutionRecord> AllRecords => _records.AsReadOnly();

    public IEnumerable<NodeExecutionRecord> SuccessfulNodes =>
        _records.Where(r => r.Status == NodeStatus.Success);

    public IEnumerable<NodeExecutionRecord> FailedNodes =>
        _records.Where(r => r.Status == NodeStatus.Failed);

    public TimeSpan TotalDuration =>
        _records.Count == 0
            ? TimeSpan.Zero
            : _records.Max(r => r.CompletedAt ?? DateTime.UtcNow) -
              _records.Min(r => r.StartedAt);

    public NodeExecutionRecord? SlowestNode =>
        _records.OrderByDescending(r => r.Duration).FirstOrDefault();

    // ─── Report ───────────────────────────────────────────────────────────────

    public WorkflowReport GenerateReport(string workflowName, string runId) => new()
    {
        WorkflowName = workflowName,
        RunId = runId,
        GeneratedAt = DateTime.UtcNow,
        TotalNodes = _records.Count,
        SuccessCount = _records.Count(r => r.Status == NodeStatus.Success),
        FailureCount = _records.Count(r => r.Status == NodeStatus.Failed),
        SkippedCount = _records.Count(r => r.Status == NodeStatus.Skipped),
        TotalDuration = TotalDuration,
        NodeBreakdown = _records.Select(r => new NodeTimingEntry
        {
            NodeName = r.NodeName,
            Category = r.NodeCategory,
            Status = r.Status,
            DurationMs = r.Duration.TotalMilliseconds,
            StartedAt = r.StartedAt,
            ErrorMessage = r.ErrorMessage
        }).ToList()
    };
}
