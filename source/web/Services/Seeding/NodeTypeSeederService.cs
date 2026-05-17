using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Repositories;
using Twf.Flow.Web.Services.Schema;
using Twf.Flow.Web.Models.VisualNodes;

namespace Twf.Flow.Web.Services.Seeding;

/// <summary>
/// Seeds (and keeps up-to-date) the NodeTypes table from the core assembly.
/// Discovery is fully reflection-driven via INodeSchemaProvider.
/// </summary>
public class NodeTypeSeederService : INodeTypeSeeder
{
    private readonly INodeSchemaProvider _schemaProvider;
    private readonly INodeTypeRepository _repository;
    private readonly ILogger<NodeTypeSeederService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public NodeTypeSeederService(
  INodeSchemaProvider schemaProvider,
   INodeTypeRepository repository,
        ILogger<NodeTypeSeederService> logger)
    {
        _schemaProvider = schemaProvider;
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting node type seeding");

        var schemas = _schemaProvider.GetAllSchemas();
        var existing = (await _repository.GetAllAsync())
     .ToDictionary(e => e.NodeType, StringComparer.OrdinalIgnoreCase);
        var overrides = GetPaletteOverrides();
        var uiOnlyDefs = GetUiOnlyNodeDefinitions();

        int upserted = 0, disabled = 0;

        // ── Upsert all nodes discovered via reflection ────────────────────────
        foreach (var schema in schemas.Values)
        {
            var nodeType = schema.NodeType;
            overrides.TryGetValue(nodeType, out var pal);

            var (category, idPrefix) = GetNodeInstanceMeta(nodeType);
            var fullTypeName = GetFullTypeName(nodeType);

            var name = pal?.Name ?? DeriveDisplayName(nodeType);
            var color = pal?.Color ?? GetCategoryColor(category);
            var icon = pal?.Icon ?? GetCategoryIcon(category);

            // Populate DataIn/DataOut from live port declarations
            var (inputs, outputs) = NodeDataMetadataProvider.GetPorts(nodeType);
            schema.DataInputs = inputs;
            schema.DataOutputs = outputs;
            var schemaJson = JsonSerializer.Serialize(schema, _jsonOptions);

            if (existing.TryGetValue(nodeType, out var entity))
            {
                entity.Name = name;
                entity.Category = category;
                entity.Description = schema.Description;
                entity.Color = color;
                entity.Icon = icon;
                entity.SchemaJson = schemaJson;
                entity.IsEnabled = true;
                entity.IdPrefix = idPrefix;
                entity.FullTypeName = fullTypeName;
                await _repository.UpdateAsync(entity);
                upserted++;
            }
            else
            {
                await _repository.CreateAsync(new NodeTypeEntity
                {
                    NodeType = nodeType,
                    Name = name,
                    Category = category,
                    Description = schema.Description,
                    Color = color,
                    Icon = icon,
                    SchemaJson = schemaJson,
                    IsEnabled = true,
                    IdPrefix = idPrefix,
                    FullTypeName = fullTypeName,
                });
                upserted++;
            }
        }

        // ── Upsert UI-only nodes (no core assembly implementation) ────────────
        foreach (var def in uiOnlyDefs)
        {
            // Get the actual schema with parameters from the schema class
            var schema = GetVisualNodeSchema(def.NodeType);
            var schemaJson = JsonSerializer.Serialize(schema, _jsonOptions);

            if (existing.TryGetValue(def.NodeType, out var entity))
            {
                entity.Name = def.Name;
                entity.Category = def.Category;
                entity.Description = def.Description;
                entity.Color = def.Color;
                entity.Icon = def.Icon;
                entity.SchemaJson = schemaJson;
                entity.IsEnabled = true;
                entity.IdPrefix = def.IdPrefix;
                entity.FullTypeName = null;
                await _repository.UpdateAsync(entity);
                upserted++;
            }
            else
            {
    await _repository.CreateAsync(new NodeTypeEntity
      {
    NodeType = def.NodeType,
         Name = def.Name,
     Category = def.Category,
        Description = def.Description,
     Color = def.Color,
          Icon = def.Icon,
  SchemaJson = schemaJson,
        IsEnabled = true,
   IdPrefix = def.IdPrefix,
  FullTypeName = null,
                });
              upserted++;
            }
        }

        // ── Disable nodes no longer present in the assembly ───────────────────
        // UI-only nodes are excluded — they have no assembly counterpart by design.
        var uiOnlyTypes = new HashSet<string>(
            uiOnlyDefs.Select(d => d.NodeType),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (nodeType, entity) in existing)
        {
            if (!schemas.ContainsKey(nodeType) && !uiOnlyTypes.Contains(nodeType) && entity.IsEnabled)
            {
                entity.IsEnabled = false;
                await _repository.UpdateAsync(entity);
                disabled++;
            }
        }

        _logger.LogInformation(
            "Node type seeding completed: {Upserted} upserted, {Disabled} disabled",
            upserted,
            disabled);
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Instantiates a skeleton node (via Dictionary constructor) to read
    /// its Category and IdPrefix � instance-level properties not in the schema.
    /// </summary>
    private (string category, string idPrefix) GetNodeInstanceMeta(string nodeType)
    {
        var type = _schemaProvider.GetNodeClass(nodeType);
        if (type is null) return ("Other", "node");

        try
        {
            var ctor = type.GetConstructor([typeof(Dictionary<string, object?>)]);
            if (ctor is not null)
            {
                var node = (INode)ctor.Invoke([new Dictionary<string, object?>()]);
                return (node.Category, node.IdPrefix);
            }
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor is not null)
            {
                var node = (INode)defaultCtor.Invoke([]);
                return (node.Category, node.IdPrefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                    "Failed to instantiate skeleton node for {NodeType} - using fallback metadata",
              nodeType);
        }

        return ("Other", "node");
    }

    private string? GetFullTypeName(string nodeType)
    {
        var type = _schemaProvider.GetNodeClass(nodeType);
        return type is null ? null : $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    /// <summary>
    /// Converts a PascalCase type name to a display name.
    /// "HttpRequestNode" ? "Http Request", "LlmNode" ? "Llm".
    /// Override via <see cref="GetPaletteOverrides"/> for custom display names.
    /// </summary>
    private static string DeriveDisplayName(string nodeType)
    {
        var name = nodeType.EndsWith("Node", StringComparison.OrdinalIgnoreCase)
                 ? nodeType[..^4]
            : nodeType;
        return Regex.Replace(name, "(?<!^)([A-Z])", " $1").Trim();
    }

    private static string GetCategoryColor(string category) => category switch
    {
        "AI" => "#4A90E2",
        "Control" => "#F5A623",
        "Data" => "#7ED321",
        "IO" => "#BD10E0",
        "Visual" => "#6366f1",
        _ => "#6c757d",
    };

    private static string GetCategoryIcon(string category) => category switch
    {
        "AI" => "bi-cpu",
        "Control" => "bi-gear",
        "Data" => "bi-table",
        "IO" => "bi-globe",
        "Visual" => "bi-bounding-box",
        _ => "bi-box",
    };

    // ?? Palette overrides (web layer only) ????????????????????????????????????

    private sealed record PaletteOverride(
        string? Name = null,
        string? Color = null,
   string? Icon = null);

    private static Dictionary<string, PaletteOverride> GetPaletteOverrides() =>
     new(StringComparer.OrdinalIgnoreCase)
     {
         // ?? Control ???????????????????????????????????????????????????????
         ["StartNode"] = new("Start", "#2ecc71", "bi-play-circle-fill"),
         ["EndNode"] = new("End", "#e74c3c", "bi-stop-circle-fill"),
         ["ErrorNode"] = new("Error Handler", "#e74c3c", "bi-exclamation-triangle-fill"),
         ["ErrorRouteNode"] = new("Error Route", "#e67e22", "bi-arrow-right-circle-fill"),
         ["ConditionNode"] = new(Icon: "bi-question-diamond"),
         ["BranchNode"] = new(Icon: "bi-signpost-split"),
         ["SubWorkflowNode"] = new("Sub Workflow", "#8e44ad", "bi-box-arrow-in-right"),
         ["LoopNode"] = new("Loop (ForEach)", "#f39c12", "bi-arrow-repeat"),
         ["DelayNode"] = new(Icon: "bi-clock"),
         ["MergeNode"] = new(Icon: "bi-intersect"),
         ["LogNode"] = new(Icon: "bi-journal-text"),

         // ?? Data ??????????????????????????????????????????????????????????
         ["SetVariableNode"] = new("Set Variable", Icon: "bi-pencil"),
         ["DataMapperNode"] = new("Data Mapper", Icon: "bi-map"),
         ["FilterNode"] = new(Icon: "bi-funnel"),
         ["ChunkTextNode"] = new("Chunk Text", Icon: "bi-file-break"),
         ["MemoryNode"] = new(Icon: "bi-memory"),

         // ?? IO ????????????????????????????????????????????????????????????
         ["FileReaderNode"] = new("File Read", Icon: "bi-file-earmark-text"),
         ["FileWriterNode"] = new("File Write", Icon: "bi-file-earmark-arrow-down"),
         ["HttpRequestNode"] = new("HTTP Request", Icon: "bi-globe"),

         // ?? AI ????????????????????????????????????????????????????????????
         ["LlmNode"] = new("LLM", Icon: "bi-chat-left-dots"),
         ["PromptBuilderNode"] = new("Prompt Builder", Icon: "bi-pencil-square"),
         ["EmbeddingNode"] = new(Icon: "bi-diagram-2"),
         ["OutputParserNode"] = new("Output Parser", Icon: "bi-list-check"),

         // ── Visual ───────────────────────────────────────────────────────────
         // ContainerNode and NoteNode are UI-only; they are seeded via
         // GetUiOnlyNodeDefinitions() and listed here only so any accidental
         // reflection hit still gets the right palette metadata.
         ["ContainerNode"] = new("Container", "#6366f1", "bi-bounding-box"),
         ["NoteNode"] = new("Note", "#f59e0b", "bi-sticky"),
     };

    // ── UI-only node definitions ──────────────────────────────────────────────
    // These nodes have no INode implementation in the core assembly.
    // They exist solely in the designer: Start/End anchor workflow execution;
    // Container and Note are purely visual.

    private sealed record UiOnlyNodeDefinition(
        string NodeType,
        string Name,
        string Category,
        string Description,
        string Color,
        string Icon,
        string IdPrefix);

    private static IReadOnlyList<UiOnlyNodeDefinition> GetUiOnlyNodeDefinitions() =>
    [
        new("StartNode",     "Start",     "Control", "Marks the entry point of the workflow.",           "#2ecc71", "bi-play-circle-fill",         "start"),
        new("EndNode",       "End",       "Control", "Marks the successful exit point of the workflow.", "#e74c3c", "bi-stop-circle-fill",         "end"),
        new("ContainerNode", "Container", "Visual",  "Groups related nodes visually on the canvas.",     "#6366f1", "bi-bounding-box",             "container"),
        new("NoteNode",      "Note",      "Visual",  "Adds a comment or annotation to the canvas.",      "#f59e0b", "bi-sticky",                   "note"),
    ];

    /// <summary>
    /// Gets the actual NodeParameterSchema for visual nodes (ContainerNode, NoteNode, etc.)
    /// </summary>
 private static NodeParameterSchema GetVisualNodeSchema(string nodeType)
  {
        return nodeType switch
     {
    "ContainerNode" => ContainerNodeSchema.GetSchema(),
        "NoteNode" => NoteNodeSchema.GetSchema(),
 "StartNode" => new NodeParameterSchema
          {
      NodeType = "StartNode",
  Description = "Marks the entry point of the workflow.",
         Parameters = [],
           DataInputs = [],
  DataOutputs = []
      },
     "EndNode" => new NodeParameterSchema
     {
        NodeType = "EndNode",
  Description = "Marks the successful exit point of the workflow.",
             Parameters = [],
 DataInputs = [],
    DataOutputs = []
  },
      _ => new NodeParameterSchema
{
       NodeType = nodeType,
    Description = "",
       Parameters = [],
           DataInputs = [],
     DataOutputs = []
            }
        };
    }
}
