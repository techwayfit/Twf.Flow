using System.Text.RegularExpressions;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Appends a string to an existing WorkflowData string value.
/// The value to append can be a literal, a WorkflowData key reference,
/// or a <c>{{variable}}</c> interpolation template.
///
/// Reads from WorkflowData:
///   - <see cref="_targetKey"/>: the string to append to (treated as empty if missing)
///   - <see cref="_appendKey"/>: (optional) key whose value is appended
///
/// Writes to WorkflowData:
///   - <see cref="_targetKey"/>: resulting concatenated string
/// </summary>
public sealed class AppendStringNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Appends a value to '{_targetKey}'";
    public override string IdPrefix => "appendstr";

    // WorkflowData keys
    public const string DefaultTargetKey = "my_text";

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(_targetKey, typeof(string), Required: false, "Base string (created empty if missing)"),
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_targetKey, typeof(string), Description: "Resulting concatenated string"),
    ];

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "AppendStringNode",
        Description = "Append a string value to an existing WorkflowData string",
        Parameters  =
        [
            new() { Name = "targetKey",   Label = "Target Key",       Type = ParameterType.Text, Required = true,  Placeholder = "my_text",
                Description = "Key of the string to append to" },
            new() { Name = "appendKey",   Label = "Append from Key",  Type = ParameterType.Text, Required = false, Placeholder = "other_value",
                Description = "WorkflowData key whose value is appended (takes priority over Append Value)" },
            new() { Name = "appendValue", Label = "Append Value",     Type = ParameterType.Text, Required = false, Placeholder = " more text",
                Description = "Literal string to append. Supports {{variable}} interpolation." },
            new() { Name = "separator",   Label = "Separator",        Type = ParameterType.Text, Required = false, Placeholder = "",
                Description = "String inserted between the base and the appended value (default: empty)" },
        ]
    };

    private readonly string  _targetKey;
    private readonly string? _appendKey;
    private readonly string? _appendValue;
    private readonly string  _separator;

    public AppendStringNode(string name, string targetKey, string? appendKey = null, string? appendValue = null, string separator = "")
    {
        Name         = name;
        _targetKey   = targetKey;
        _appendKey   = appendKey;
        _appendValue = appendValue;
        _separator   = separator;
    }

    public AppendStringNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")        ?? "Append String",
            NodeParameters.GetString(parameters, "targetKey")   ?? DefaultTargetKey,
            NodeParameters.GetString(parameters, "appendKey"),
            NodeParameters.GetString(parameters, "appendValue"),
            NodeParameters.GetString(parameters, "separator")   ?? "")
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var baseStr = input.GetString(_targetKey) ?? string.Empty;

        string appendPart=string.Empty;
        if (!string.IsNullOrWhiteSpace(_appendKey))
        {
            appendPart = input.NestedValue(_appendKey)?.ToString() ?? string.Empty;
            nodeCtx.Log($"Appending value from key '{_appendKey}'");
        }
       /* else
        {
            appendPart = Interpolate(_appendValue ?? string.Empty, input);
            nodeCtx.Log($"Appending literal value");
        }
*/
        var result = string.IsNullOrEmpty(appendPart)
            ? baseStr
            : string.IsNullOrEmpty(baseStr)
                ? appendPart
                : baseStr + _separator + appendPart;

        var output = input.Set(_targetKey, result);

        nodeCtx.Log($"'{_targetKey}': {baseStr.Length} chars → {result.Length} chars");
        nodeCtx.SetMetadata("original_length", baseStr.Length);
        nodeCtx.SetMetadata("result_length",   result.Length);

        return Task.FromResult(output);
    }

    private static string Interpolate(string template, WorkflowData data) =>
        Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
        {
            var key = m.Groups[1].Value;
            return data.TryGet<object>(key, out var val) && val is not null
                ? val.ToString()!
                : m.Value;
        });
}
