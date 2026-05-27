using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Appends or prepends an item to an existing list in WorkflowData.
/// The item can be a literal value or taken from another WorkflowData key.
///
/// Reads from WorkflowData:
///   - <see cref="_listKey"/>: existing list (created if absent)
///   - <see cref="_itemKey"/>: (optional) key whose value is used as the item
///
/// Writes to WorkflowData:
///   - <see cref="_listKey"/>: updated list
///   - list_count: new length
/// </summary>
public sealed class AddListItemNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Adds an item to list '{_listKey}'";

    // WorkflowData keys
    public const string OutputListCount = "list_count";

    private readonly string  _listKey;
    private readonly string? _itemKey;
    private readonly string? _itemValue;
    private readonly bool    _prepend;

    public AddListItemNode(string name, string listKey, string? itemKey = null, string? itemValue = null, bool prepend = false)
    {
        Name       = name;
        _listKey   = listKey;
        _itemKey   = itemKey;
        _itemValue = itemValue;
        _prepend   = prepend;
    }

    public AddListItemNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Add List Item",
            NodeParameters.GetString(parameters, "listKey") ?? "my_list",
            NodeParameters.GetString(parameters, "itemKey"),
            NodeParameters.GetString(parameters, "itemValue"),
            NodeParameters.GetString(parameters, "position") == "start")
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var list = ResolveList(input, _listKey);

        // Determine what to add
        object? item;
        if (!string.IsNullOrWhiteSpace(_itemKey) && input.Has(_itemKey))
        {
            item = input.Get<object>(_itemKey);
            nodeCtx.Log($"Adding item from key '{_itemKey}'");
        }
        else
        {
            item = _itemValue;
            nodeCtx.Log($"Adding literal value '{_itemValue}'");
        }

        if (_prepend)
            list.Insert(0, item);
        else
            list.Add(item);

        var output = input.Clone()
            .Set(_listKey,        list)
            .Set(OutputListCount, list.Count);

        nodeCtx.SetMetadata("list_key",   _listKey);
        nodeCtx.SetMetadata("list_count", list.Count);
        return Task.FromResult(output);
    }

    internal static List<object?> ResolveList(WorkflowData data, string key)
    {
        if (!data.Has(key)) return [];
        var raw = data.Get<object>(key);

        if (raw is List<object?> list)        return new List<object?>(list);
        if (raw is List<object> objList)      return objList.Cast<object?>().ToList();

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => (object?)CreateListNode.BoxJsonElement(e)).ToList();

        // Single value wrapped in a list
        return [raw];
    }
}
