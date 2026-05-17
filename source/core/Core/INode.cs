namespace Twf.Flow.Core;

/// <summary>
/// Describes a single input or output data slot on a node.
/// Used by the UI to show available variables and by the runner to validate data flow.
/// </summary>
/// <param name="Key">WorkflowData key this port reads from / writes to.</param>
/// <param name="DataType">CLR type of the value (typeof(string), typeof(int), etc.).</param>
/// <param name="Required">If true the runner will fail before executing if the key is absent.</param>
/// <param name="Description">Human-readable hint shown in the UI.</param>
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

    /// <summary>
    /// Short prefix used to generate human-readable node IDs in the designer
    /// (e.g. "llm" → llm001, llm002). Must be lowercase, letters only.
    /// </summary>
    string IdPrefix { get; }

    /// <summary>
    /// WorkflowData keys this node reads. Required ports are validated by the runner
    /// before execution; missing keys produce a clear error instead of a silent null.
    /// </summary>
    IReadOnlyList<NodeData> DataIn { get; }

    /// <summary>
    /// WorkflowData keys this node writes. The runner also writes each key under
    /// the scoped form "nodeId.key" so downstream nodes can reference a specific
    /// node's output via {{nodeId.key}} even when multiple nodes write the same key.
    /// </summary>
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
