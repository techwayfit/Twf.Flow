using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Control;

// ═══════════════════════════════════════════════════════════════════════════════
// MergeNode — Merge multiple data keys into a single value
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Merges multiple WorkflowData keys into a single aggregated value.
/// Useful for combining results from parallel branches or multiple sources.
/// </summary>
public sealed class MergeNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Control";
    public override string Description => Schema.Description;

    /// <inheritdoc/>
    public override string IdPrefix => "merge";

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataIn =>
        _sourceKeys.Select(k => new NodeData(k, typeof(string), Required: false, "Source key to merge"))
                   .ToList<NodeData>();

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_outputKey, typeof(string), Description: "Merged result")
    ];

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "MergeNode",
        Description = "Concatenate multiple data keys into one output key",
        Parameters  =
        [
            new() { Name = "sourceKeys", Label = "Source Keys (JSON array)", Type = ParameterType.Json,   Required = true,  Placeholder = "[\"key1\", \"key2\", \"key3\"]" },
            new() { Name = "outputKey",  Label = "Output Key",               Type = ParameterType.Text,   Required = true,  Placeholder = "merged_output" },
            new() { Name = "separator",  Label = "Separator",                Type = ParameterType.Text,   Required = false, DefaultValue = "\n" },
        ]
    };

    private readonly string[] _sourceKeys;
    private readonly string _outputKey;
    private readonly string _separator;

    public MergeNode(string name, string outputKey, string separator = "\n",
        params string[] sourceKeys)
    {
        Name = name;
        _outputKey = outputKey;
        _separator = separator;
        _sourceKeys = sourceKeys;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public MergeNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Merge",
            NodeParameters.GetString(parameters, "outputKey") ?? "merged",
            NodeParameters.GetString(parameters, "separator") ?? "\n",
            (NodeParameters.GetStringList(parameters, "sourceKeys") ?? []).ToArray())
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var parts = _sourceKeys
            .Select(k => input.Get<object>(k)?.ToString())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        var merged = string.Join(_separator, parts);
        nodeCtx.Log($"Merged {parts.Count}/{_sourceKeys.Length} keys into '{_outputKey}'");

        return Task.FromResult(input.Clone().Set(_outputKey, merged));
    }
}