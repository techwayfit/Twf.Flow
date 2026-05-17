using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Twf.Flow.Web.Models;

/// <summary>
/// Represents a complete workflow definition with nodes and connections
/// </summary>
public class WorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<NodeDefinition> Nodes { get; set; } = new();
    
    public List<ConnectionDefinition> Connections { get; set; } = new();

    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Workflow-level error handler node reference (optional, max one in UI).
    /// </summary>
    public Guid? ErrorNodeId { get; set; }

    /// <summary>
    /// Child workflows that belong to this root workflow.
    /// Each child workflow can be called from SubWorkflowNode.
    /// </summary>
    public List<ChildWorkflowDefinition> SubWorkflows { get; set; } = new();

    public WorkflowMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Represents a single node in the workflow
/// </summary>
public class NodeDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Short human-readable ID unique within the workflow (e.g. "llm001", "http002").
    /// Assigned by the designer client when a node is first dropped onto the canvas.
    /// Stable for the life of the node — used in {{nodeId.key}} template references.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

  [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty; // LlmNode, PromptBuilderNode, etc.

    public string Category { get; set; } = string.Empty; // AI, Data, IO, Control

    public Dictionary<string, object?> Parameters { get; set; } = new();

    // Visual editor properties
    public NodePosition Position { get; set; } = new();

    public string? Color { get; set; }
    
  // Phase 1: Execution options
    public NodeExecutionOptions? ExecutionOptions { get; set; }
  
    // Phase 5: Sub-workflow support for container nodes (Loop, Parallel, Branch)
    public SubWorkflowDefinition? SubWorkflow { get; set; }
    public bool IsExpanded { get; set; } = false;
}

/// <summary>
/// Execution options for a specific node instance
/// Phase 1: Node execution configuration
/// </summary>
public class NodeExecutionOptions
{
    public int MaxRetries { get; set; } = 0;
    public int RetryDelayMs { get; set; } = 1000;
    public int? TimeoutMs { get; set; }
    public bool ContinueOnError { get; set; } = false;
    public string? RunCondition { get; set; } // e.g., "{{should_run}} == true"
 public Dictionary<string, object?>? FallbackData { get; set; }
}

/// <summary>
/// Represents a connection between two nodes
/// </summary>
public class ConnectionDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SourceNodeId { get; set; }

    public string SourcePort { get; set; } = "output";

    [Required]
    public Guid TargetNodeId { get; set; }

    public string TargetPort { get; set; } = "input";

    public string? Label { get; set; }
}

/// <summary>
/// Node position on the canvas
/// </summary>
public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Workflow metadata
/// </summary>
public class WorkflowMetadata
{
  public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();
    public int Version { get; set; } = 1;
 public bool IsActive { get; set; } = true;
}

/// <summary>
/// Defines a sub-workflow within a container node
/// Phase 5: Loop, Parallel, and Branch node support
/// </summary>
public class SubWorkflowDefinition
{
    public List<NodeDefinition> Nodes { get; set; } = new();
    public List<ConnectionDefinition> Connections { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// Defines a reusable child workflow under a root workflow.
/// </summary>
public class ChildWorkflowDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "Sub Workflow";

    [StringLength(1000)]
    public string? Description { get; set; }

    public List<NodeDefinition> Nodes { get; set; } = new();
    public List<ConnectionDefinition> Connections { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();

    // Workflow-level error handler node reference (optional, max one in UI).
    public Guid? ErrorNodeId { get; set; }
}
