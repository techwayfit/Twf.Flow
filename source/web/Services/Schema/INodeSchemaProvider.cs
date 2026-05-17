using Twf.Flow.Core;

namespace Twf.Flow.Web.Services.Schema;

/// <summary>
/// Service for discovering and accessing node schema definitions.
/// Provides node parameter schemas and node class types for dynamic instantiation.
/// </summary>
public interface INodeSchemaProvider
{
    /// <summary>
  /// Gets all discovered node schemas keyed by node type name.
    /// </summary>
    /// <returns>Dictionary of NodeType ? NodeParameterSchema</returns>
    Dictionary<string, NodeParameterSchema> GetAllSchemas();

    /// <summary>
    /// Gets the CLR Type for a given node type name.
    /// </summary>
  /// <param name="nodeType">The node type name (e.g., "LlmNode")</param>
    /// <returns>The node's CLR Type, or null if not found</returns>
    Type? GetNodeClass(string nodeType);

    /// <summary>
    /// Gets the schema for a specific node type.
 /// </summary>
    /// <param name="nodeType">The node type name</param>
    /// <returns>The node's parameter schema, or null if not found</returns>
    NodeParameterSchema? GetSchema(string nodeType);

    /// <summary>
    /// Gets all registered node type names.
/// </summary>
    /// <returns>Collection of node type names</returns>
    IReadOnlyCollection<string> GetRegisteredNodeTypes();
}
