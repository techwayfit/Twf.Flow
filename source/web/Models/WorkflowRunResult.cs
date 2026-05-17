using Twf.Flow.Core;

namespace Twf.Flow.Web.Models;

/// <summary>
/// Result of executing a <see cref="WorkflowDefinition"/> via
/// <see cref="Twf.Flow.Web.Services.WorkflowDefinitionRunner"/>.
/// </summary>
public sealed class WorkflowRunResult
{
    public bool IsSuccess { get; init; }
    public string WorkflowName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? FailedNodeName { get; init; }

    /// <summary>Final WorkflowData after execution, serialised to a dictionary for JSON responses.</summary>
    public Dictionary<string, object?> OutputData { get; init; } = new();

    public DateTime FinishedAt { get; init; } = DateTime.UtcNow;

    public static WorkflowRunResult Success(string workflowName, WorkflowData data) => new()
    {
        IsSuccess    = true,
        WorkflowName = workflowName,
        OutputData   = data.Keys.ToDictionary(k => k, k => data.Get<object>(k))
    };

    public static WorkflowRunResult Failure(
        string workflowName, WorkflowData data, string? error, string? failedNode) => new()
    {
        IsSuccess      = false,
        WorkflowName   = workflowName,
        ErrorMessage   = error,
        FailedNodeName = failedNode,
        OutputData     = data.Keys.ToDictionary(k => k, k => data.Get<object>(k))
    };
}

/// <summary>
/// Optional request body for POST /Workflow/Run/{id} and POST /Workflow/RunStream/{id}.
/// Lets callers seed initial WorkflowData values before execution starts.
/// </summary>
public sealed class WorkflowRunRequest
{
    /// <summary>
    /// Key-value pairs written into the initial WorkflowData before the first node runs.
    /// Example: { "user_message": "Hello!", "language": "English" }
    /// </summary>
    public Dictionary<string, object?> InitialData { get; set; } = new();
}

/// <summary>
/// Emitted by <see cref="Twf.Flow.Web.Services.WorkflowDefinitionRunner"/> after
/// each node starts or finishes executing. Serialised as a Server-Sent Event payload for
/// the Runner UI.
/// </summary>
public sealed class NodeStepEvent
{
    /// <summary>"node_start" | "node_done" | "node_error" | "workflow_done" | "workflow_error"</summary>
    public string EventType { get; init; } = string.Empty;
    public Guid NodeId { get; init; }
    /// <summary>Short human-readable node ID (e.g. "llm001"). Used for {{nodeId.key}} references.</summary>
    public string NodeRefId { get; init; } = string.Empty;
    public string NodeName { get; init; } = string.Empty;
    public string NodeType { get; init; } = string.Empty;
    /// <summary>Snapshot of WorkflowData BEFORE the node executed (filtered to DataIn keys).</summary>
    public Dictionary<string, object?> InputData { get; init; } = new();
    /// <summary>Snapshot of WorkflowData AFTER the node executed (filtered to DataOut keys).</summary>
    public Dictionary<string, object?> OutputData { get; init; } = new();
    /// <summary>True when the node declares at least one DataIn port.</summary>
    public bool DataInConfigured { get; init; }
    /// <summary>True when the node declares at least one DataOut port.</summary>
    public bool DataOutConfigured { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
