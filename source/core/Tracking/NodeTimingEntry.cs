using Twf.Flow.Core;

namespace Twf.Flow.Tracking;

public sealed class NodeTimingEntry
{
    public string NodeName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public NodeStatus Status { get; init; }
    public double DurationMs { get; init; }
    public DateTime StartedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
