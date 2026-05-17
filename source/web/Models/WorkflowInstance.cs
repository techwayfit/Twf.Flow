namespace Twf.Flow.Web.Models;

public enum WorkflowRunStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Represents a single execution run of a <see cref="WorkflowDefinition"/>.
/// Created before the run starts and updated as each node executes.
/// </summary>
public class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public WorkflowRunStatus Status { get; set; } = WorkflowRunStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object?> InitialData { get; set; } = new();
    public Dictionary<string, object?> OutputData { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FailedNodeName { get; set; }

    /// <summary>Ordered log of every node_start / node_done / node_error event.</summary>
    public List<NodeStepEvent> Steps { get; set; } = new();
}
