using Twf.Flow.Core;
using Twf.Flow.Web.Models.VisualNodes;

namespace Twf.Flow.Web.Services;

/// <summary>
/// Creates a minimal skeleton instance of each node type and reads its
/// DataIn / DataOut directly from the core implementation.
/// For visual nodes (no CLR type), returns ports defined in their schemas.
/// Uses the INodeSchemaProvider registry — no switch-case required.
/// </summary>
public static class NodeDataMetadataProvider
{
    /// <summary>
    /// Returns (DataInputs, DataOutputs) for the given node type string,
    /// or empty lists if the type is unknown or has no declared ports.
    /// </summary>
    public static (List<DataPortInfo> inputs, List<DataPortInfo> outputs) GetPorts(
        string nodeType)
    {
        // Handle visual nodes (no CLR type, but have schemas in web assembly)
        var visualNodePorts = GetVisualNodePorts(nodeType);
        if (visualNodePorts.HasValue)
        {
            return visualNodePorts.Value;
        }

        // Note: This method is called during seeding, so we can't inject INodeSchemaProvider
        // We need to use reflection directly on the core assembly
        var type = GetNodeTypeFromCoreAssembly(nodeType);
        if (type is null) return ([], []);

        try
        {
            var ctor = type.GetConstructor(new[] { typeof(Dictionary<string, object?>) });
            if (ctor is null) return ([], []);

            var node = (INode)ctor.Invoke(new object[] { new Dictionary<string, object?>() });
            return (
                node.DataIn .Select(ToPortInfo).ToList(),
                node.DataOut.Select(ToPortInfo).ToList()
            );
        }
        catch
        {
            return ([], []);
        }
    }

    /// <summary>
    /// Gets CLR type from core assembly for executable nodes.
    /// Returns null for visual nodes (which is expected).
    /// </summary>
    private static Type? GetNodeTypeFromCoreAssembly(string nodeType)
    {
        return typeof(TwfAiFramework.Nodes.BaseNode).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INode).IsAssignableFrom(t))
            .FirstOrDefault(t =>
            {
                var schema = t.GetProperty("Schema", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
              ?.GetValue(null) as NodeParameterSchema;
                return schema?.NodeType?.Equals(nodeType, StringComparison.OrdinalIgnoreCase) == true;
            });
    }

    /// <summary>
    /// Returns ports for visual nodes (UI-only nodes with no backend implementation).
    /// Returns null if the node is not a visual node.
    /// </summary>
    private static (List<DataPortInfo> inputs, List<DataPortInfo> outputs)? GetVisualNodePorts(string nodeType)
    {
        NodeParameterSchema? schema = nodeType switch
        {
            "ContainerNode" => ContainerNodeSchema.GetSchema(),
            "NoteNode" => NoteNodeSchema.GetSchema(),
            _ => null
        };

        if (schema == null) return null;

        // Visual nodes have their ports defined in the schema
        return (schema.DataInputs ?? [], schema.DataOutputs ?? []);
    }

    private static DataPortInfo ToPortInfo(NodeData p) => new()
    {
        Key    = p.Key,
        Required    = p.Required,
        IsDynamic   = false,
        Description = p.Description,
    };
}
