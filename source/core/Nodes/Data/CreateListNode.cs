using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Creates a new list and stores it in WorkflowData under the specified key.
/// Optionally pre-populated from a JSON array literal or from an existing WorkflowData key.
///
/// Writes to WorkflowData:
///   - <see cref="_listKey"/>: the newly created list
///   - list_count: number of items in the created list
/// </summary>
public sealed class CreateListNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Creates a list stored as '{_listKey}'";
    public override string IdPrefix => "list";

    // WorkflowData keys
    public const string DefaultListKey  = "my_list";
    public const string OutputListCount = "list_count";

    public override IReadOnlyList<NodeData> DataIn  => [];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_listKey,        typeof(List<object?>), Description: "The created list"),
        new(OutputListCount, typeof(int),           Description: "Number of items in the list"),
    ];

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "CreateListNode",
        Description = "Create a new list, optionally pre-populated from a JSON array",
        Parameters  =
        [
            new() { Name = "listKey",      Label = "List Key",          Type = ParameterType.Text,  Required = true,  Placeholder = "my_list",
                Description = "WorkflowData key where the list is stored" },
            new() { Name = "initialItems", Label = "Initial Items (JSON Array)", Type = ParameterType.Json, Required = false, Placeholder = "[\"item1\", \"item2\"]",
                Description = "Optional JSON array to pre-populate the list" },
        ]
    };

    private readonly string _listKey;
    private readonly List<object?> _initial;

    public CreateListNode(string name, string listKey, List<object?>? initial = null)
    {
        Name     = name;
        _listKey = listKey;
        _initial = initial ?? [];
    }

    public CreateListNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Create List",
            NodeParameters.GetString(parameters, "listKey") ?? DefaultListKey,
            ParseInitialItems(parameters))
    { }

    private static List<object?> ParseInitialItems(Dictionary<string, object?> parameters)
    {
        var raw = parameters.GetValueOrDefault("initialItems");
        if (raw is null) return [];

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => (object?)BoxJsonElement(e)).ToList();

        if (raw is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(s);
                return parsed?.Select(e => (object?)BoxJsonElement(e)).ToList() ?? [];
            }
            catch { /* fall through */ }
        }

        return [];
    }

    internal static object? BoxJsonElement(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String  => e.GetString(),
        JsonValueKind.Number  => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => null,
        _                     => e,
    };

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var list = new List<object?>(_initial);
        var output = input.Clone()
            .Set(_listKey,        list)
            .Set(OutputListCount, list.Count);

        nodeCtx.Log($"Created list '{_listKey}' with {list.Count} item(s)");
        nodeCtx.SetMetadata("list_key",   _listKey);
        nodeCtx.SetMetadata("list_count", list.Count);

        return Task.FromResult(output);
    }
}
