using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Merges two lists from WorkflowData into a single output list.
///
/// Reads from WorkflowData:
///   - <see cref="_listKeyA"/>: first list
///   - <see cref="_listKeyB"/>: second list
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: merged result
///   - list_count: total item count
/// </summary>
public sealed class MergeListsNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Merges '{_listKeyA}' + '{_listKeyB}' → '{_outputKey}'";

    // WorkflowData keys
    public const string OutputListCount = "list_count";

    private readonly string _listKeyA;
    private readonly string _listKeyB;
    private readonly string _outputKey;
    private readonly bool   _deduplicate;

    public MergeListsNode(string name, string listKeyA, string listKeyB, string outputKey, bool deduplicate = false)
    {
        Name         = name;
        _listKeyA    = listKeyA;
        _listKeyB    = listKeyB;
        _outputKey   = outputKey;
        _deduplicate = deduplicate;
    }

    public MergeListsNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "Merge Lists",
            NodeParameters.GetString(parameters, "listKeyA")  ?? "list_a",
            NodeParameters.GetString(parameters, "listKeyB")  ?? "list_b",
            NodeParameters.GetString(parameters, "outputKey") ?? "merged_list",
            NodeParameters.GetBool(parameters, "deduplicate"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var listA  = AddListItemNode.ResolveList(input, _listKeyA);
        var listB  = AddListItemNode.ResolveList(input, _listKeyB);
        var merged = listA.Concat(listB).ToList();

        if (_deduplicate)
        {
            var seen     = new HashSet<string>();
            var deduped  = new List<object?>();
            foreach (var item in merged)
            {
                var key = item?.ToString() ?? "__null__";
                if (seen.Add(key)) deduped.Add(item);
            }
            merged = deduped;
            nodeCtx.Log($"Deduplicated: {listA.Count + listB.Count} → {merged.Count} items");
        }

        var output = input.Clone()
            .Set(_outputKey,      merged)
            .Set(OutputListCount, merged.Count);

        nodeCtx.Log($"Merged '{_listKeyA}' ({listA.Count}) + '{_listKeyB}' ({listB.Count}) → '{_outputKey}' ({merged.Count})");
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.MergedCount, merged.Count);
        return Task.FromResult(output);
    }
}
