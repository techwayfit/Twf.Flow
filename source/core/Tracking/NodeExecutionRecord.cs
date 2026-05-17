using Twf.Flow.Core;

namespace Twf.Flow.Tracking;

/// <summary>Mutable record of a single node's execution.</summary>
public sealed class NodeExecutionRecord
{
    public string NodeName { get; init; } = string.Empty;
    public string NodeCategory { get; init; } = string.Empty;
    public NodeStatus Status { get; set; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> Logs { get; set; } = new();
}
