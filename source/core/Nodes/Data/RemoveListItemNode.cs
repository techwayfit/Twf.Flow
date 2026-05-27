using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Removes an item from a list by index or by matching a value.
///
/// Reads from WorkflowData:
///   - <see cref="_listKey"/>: the list to modify
///
/// Writes to WorkflowData:
///   - <see cref="_listKey"/>: updated list (item removed)
///   - list_count:   new length
///   - removed_item: the item that was removed (or null if nothing matched)
/// </summary>
public sealed class RemoveListItemNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Removes an item from list '{_listKey}'";

    // WorkflowData keys
    public const string OutputListCount  = "list_count";
    public const string OutputRemovedItem = "removed_item";

    private readonly string  _listKey;
    private readonly int?    _removeIndex;    // null means "not specified"
    private readonly string? _removeValue;

    public RemoveListItemNode(string name, string listKey, int? removeIndex = null, string? removeValue = null)
    {
        Name         = name;
        _listKey     = listKey;
        _removeIndex = removeIndex;
        _removeValue = removeValue;
    }

    public RemoveListItemNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Remove List Item",
            NodeParameters.GetString(parameters, "listKey") ?? "my_list",
            ParseIndex(parameters),
            NodeParameters.GetString(parameters, "removeValue"))
    { }

    private static int? ParseIndex(Dictionary<string, object?> p)
    {
        var raw = NodeParameters.GetInt(p, "removeIndex", -999);
        return raw == -999 ? null : raw;
    }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var list    = AddListItemNode.ResolveList(input, _listKey);
        object? removed = null;
        bool    found   = false;

        if (_removeIndex.HasValue && list.Count > 0)
        {
            // Support negative indexing: -1 = last element
            var idx = _removeIndex.Value < 0
                ? list.Count + _removeIndex.Value
                : _removeIndex.Value;

            if (idx >= 0 && idx < list.Count)
            {
                removed = list[idx];
                list.RemoveAt(idx);
                found = true;
                nodeCtx.Log($"Removed item at index {idx}");
            }
            else
            {
                nodeCtx.Log($"⚠️ Index {_removeIndex.Value} is out of bounds (list length {list.Count})");
            }
        }
        else if (!string.IsNullOrEmpty(_removeValue))
        {
            var idx = list.FindIndex(item => item?.ToString() == _removeValue);
            if (idx >= 0)
            {
                removed = list[idx];
                list.RemoveAt(idx);
                found = true;
                nodeCtx.Log($"Removed item with value '{_removeValue}' at index {idx}");
            }
            else
            {
                nodeCtx.Log($"⚠️ Value '{_removeValue}' not found in list");
            }
        }

        var output = input.Clone()
            .Set(_listKey,         list)
            .Set(OutputListCount,  list.Count)
            .Set(OutputRemovedItem, removed);

        nodeCtx.SetMetadata("removed", found);
        nodeCtx.SetMetadata("list_count", list.Count);
        return Task.FromResult(output);
    }
}
