using System.Reflection;
using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes;
using Twf.Flow.Web.Models.VisualNodes;

namespace Twf.Flow.Web.Services.Schema;

/// <summary>
/// Discovers NodeParameterSchema definitions by scanning:
/// 1. Core assembly for INode subclasses with static Schema property
/// 2. Web assembly for visual node schemas (UI-only nodes without business logic)
/// 
/// No manual registration is required � adding a Schema property to a node class
/// or a static schema in the web layer is sufficient.
/// The assemblies are scanned once (lazily) and the results are cached for the lifetime
/// of the service (Singleton).
/// </summary>
public class ReflectionNodeSchemaProvider : INodeSchemaProvider
{
    private readonly ILogger<ReflectionNodeSchemaProvider> _logger;

    /// <summary>
    /// Lazy-loaded discovered schemas. Thread-safe initialization.
    /// </summary>
    private readonly Lazy<IReadOnlyList<(NodeParameterSchema Schema, Type? NodeClass)>> _discovered;

    public ReflectionNodeSchemaProvider(ILogger<ReflectionNodeSchemaProvider> logger)
  {
  _logger = logger;
        _discovered = new Lazy<IReadOnlyList<(NodeParameterSchema Schema, Type? NodeClass)>>(
    DiscoverSchemas,
     LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Dictionary<string, NodeParameterSchema> GetAllSchemas()
    {
        return _discovered.Value.ToDictionary(x => x.Schema.NodeType, x => x.Schema);
    }

    public Type? GetNodeClass(string nodeType)
    {
 return _discovered.Value
            .FirstOrDefault(x => x.Schema.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase))
     .NodeClass;
  }

    public NodeParameterSchema? GetSchema(string nodeType)
    {
      return _discovered.Value
   .FirstOrDefault(x => x.Schema.NodeType.Equals(nodeType, StringComparison.OrdinalIgnoreCase))
            .Schema;
    }

    public IReadOnlyCollection<string> GetRegisteredNodeTypes()
    {
   return _discovered.Value.Select(x => x.Schema.NodeType).ToList();
    }

    /// <summary>
    /// Discovers all node schemas via reflection.
    /// Called once when the Lazy is first accessed.
  /// </summary>
    private IReadOnlyList<(NodeParameterSchema Schema, Type? NodeClass)> DiscoverSchemas()
    {
      _logger.LogInformation("Starting node schema discovery from core assembly");

        // 1. Scan core assembly for executable nodes
        var coreNodes = typeof(BaseNode).Assembly
    .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INode).IsAssignableFrom(t))
    .Select(t => (
       schema: t.GetProperty("Schema", BindingFlags.Public | BindingFlags.Static)
         ?.GetValue(null) as NodeParameterSchema,
           type: (Type?)t
    ))
.Where(x => x.schema is not null)
          .Select(x => (x.schema!, x.type))
            .ToList();

     _logger.LogInformation(
  "Core node schema discovery completed: {Count} executable nodes discovered",
     coreNodes.Count);

        // 2. Add visual nodes from web assembly (no CLR type - they're UI-only)
        var visualNodes = new List<(NodeParameterSchema Schema, Type? NodeClass)>
        {
            (ContainerNodeSchema.GetSchema(), null),
            (NoteNodeSchema.GetSchema(), null)
      };

    _logger.LogInformation(
            "Visual node schemas added: {Count} UI-only nodes",
        visualNodes.Count);

        var allSchemas = coreNodes.Concat(visualNodes).ToList();

        _logger.LogInformation(
    "Total node schema discovery completed: {Total} schemas ({Core} executable + {Visual} visual)",
          allSchemas.Count,
            coreNodes.Count,
         visualNodes.Count);

        return allSchemas;
    }
}
