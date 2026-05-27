namespace Twf.Flow.Core;

/// <summary>
/// Describes a single input or output data slot on a node.
/// Used for optional metadata/validation and explicit data contracts.
/// </summary>
/// <param name="Key">WorkflowData key this port reads from / writes to.</param>
/// <param name="DataType">CLR type of the value (typeof(string), typeof(int), etc.).</param>
/// <param name="Required">If true the key is expected by the node contract.</param>
/// <param name="Description">Human-readable hint.</param>
public record NodeData(
    string Key,
    Type   DataType,
    bool   Required    = true,
    string Description = "");

/// <summary>
/// The fundamental unit of work in Twf.Flow — equivalent to a node in n8n.
/// Every reusable operation (LLM call, HTTP request, transform, etc.) implements this.
/// </summary>
public interface INode
{
    /// <summary>Human-readable name shown in logs and execution reports.</summary>
    string Name { get; }

    /// <summary>Category grouping: AI, Data, IO, Control, etc.</summary>
    string Category { get; }

    /// <summary>A short description of what this node does.</summary>
    string Description { get; }

    /// <summary>Short prefix used for node identity metadata.</summary>
    string IdPrefix { get; }

    /// <summary>WorkflowData keys this node reads.</summary>
    IReadOnlyList<NodeData> DataIn { get; }

    /// <summary>WorkflowData keys this node writes.</summary>
    IReadOnlyList<NodeData> DataOut { get; }

    /// <summary>
    /// Execute this node. Receives the current data packet and execution context.
    /// Returns a NodeResult containing the updated data and execution metadata.
    /// </summary>
    Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context);
}

/// <summary>
/// Marker interface for nodes that can validate their configuration before execution.
/// </summary>
public interface IValidatableNode : INode
{
    /// <summary>Validate node configuration. Throws if invalid.</summary>
    void Validate();
}

/// <summary>Execution status of a node.</summary>
public enum NodeStatus
{
    Pending,
    Running,
    Success,
    Failed,
    Skipped,
    TimedOut,
    Cancelled
}
