using Twf.Flow.Core;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Services.NodeFactory;

/// <summary>
/// Service responsible for creating INode instances from workflow node definitions.
/// Encapsulates node type discovery and instantiation logic.
/// </summary>
public interface INodeFactory
{
    /// <summary>
    /// Creates a node instance from a node definition, with parameter variable resolution.
    /// </summary>
    /// <param name="nodeDefinition">The node definition from the workflow.</param>
    /// <param name="workflowData">Current workflow data for variable substitution.</param>
    /// <returns>An instantiated INode, or null if the node type is not found or cannot be created.</returns>
    INode? CreateNode(NodeDefinition nodeDefinition, WorkflowData workflowData);

  /// <summary>
  /// Checks if a node type is registered and can be instantiated.
    /// </summary>
    /// <param name="nodeType">The node type name (e.g., "LlmNode").</param>
  /// <returns>True if the node type is registered; otherwise, false.</returns>
    bool IsNodeTypeRegistered(string nodeType);

    /// <summary>
    /// Gets all registered node types.
    /// </summary>
    /// <returns>A collection of registered node type names.</returns>
    IReadOnlyCollection<string> GetRegisteredNodeTypes();
}
