using Twf.Flow.Core;

namespace Twf.Flow.Tracking;

/// <summary>Immutable report generated at the end of a workflow run.</summary>
public sealed class WorkflowReport
{
    public string WorkflowName { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public int TotalNodes { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public int SkippedCount { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public List<NodeTimingEntry> NodeBreakdown { get; init; } = new();

    public string ToTable()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"\n📊 Workflow Execution Report: {WorkflowName} [{RunId}]");
        sb.AppendLine($"   Total: {TotalNodes} nodes | ✅ {SuccessCount} | ❌ {FailureCount} | ⏭ {SkippedCount}");
        sb.AppendLine($"   Duration: {TotalDuration.TotalMilliseconds:F0}ms\n");
        sb.AppendLine($"   {"Node",-35} {"Category",-12} {"Status",-10} {"Duration",10}");
        sb.AppendLine($"   {new string('─', 75)}");
        foreach (var e in NodeBreakdown)
        {
            var icon = e.Status switch
            {
                NodeStatus.Success => "✅",
                NodeStatus.Failed => "❌",
                NodeStatus.Skipped => "⏭",
                _ => "⏳"
            };
            sb.AppendLine($"   {icon} {e.NodeName,-33} {e.Category,-12} {e.Status,-10} {e.DurationMs,8:F0}ms");
            if (e.ErrorMessage is not null)
                sb.AppendLine($"      ↳ Error: {e.ErrorMessage}");
        }
        return sb.ToString();
    }
}
