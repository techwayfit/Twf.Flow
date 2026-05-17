using System.Reflection;
using Twf.Flow.Core;
using Twf.Flow.Nodes;

namespace Twf.Flow.Web.Services;

/// <summary>
/// Discovers NodeParameterSchema definitions by scanning the core assembly for
/// every INode subclass that exposes a static <c>Schema</c> property.
/// No manual registration is required — adding a Schema property to a node class
/// is sufficient.
/// The assembly is scanned once (Lazy) and the results are shared across callers.
/// </summary>
public static class NodeSchemaProvider
{
    // Single assembly scan — cached for the lifetime of the process.
    private static readonly Lazy<IReadOnlyList<(NodeParameterSchema Schema, Type NodeClass)>> _discovered
        = new(() => typeof(BaseNode).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INode).IsAssignableFrom(t))
            .Select(t => (
                schema: t.GetProperty("Schema", BindingFlags.Public | BindingFlags.Static)
                          ?.GetValue(null) as NodeParameterSchema,
                type: t
            ))
            .Where(x => x.schema is not null)
            .Select(x => (x.schema!, x.type))
            .ToList());

    /// <summary>Returns all discovered schemas keyed by NodeType.</summary>
    public static Dictionary<string, NodeParameterSchema> GetAllSchemas()
        => _discovered.Value.ToDictionary(x => x.Schema.NodeType, x => x.Schema);

    /// <summary>
    /// Returns the CLR Type for a given NodeType string, or null if not found.
    /// Keyed by Schema.NodeType so "FileReaderNode" correctly resolves to FileReaderNode.
    /// </summary>
    public static Type? GetNodeClass(string nodeType)
        => _discovered.Value
            .FirstOrDefault(x => x.Schema.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase))
            .NodeClass;
}
