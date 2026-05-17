using System.Text.RegularExpressions;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

// ═══════════════════════════════════════════════════════════════════════════════
// SetVariableNode — Write literal or interpolated values into WorkflowData
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Writes one or more key/value pairs into WorkflowData.
/// String values support {{variable}} interpolation against the current WorkflowData.
///
/// Reads from WorkflowData:
///   - Any keys referenced inside {{…}} placeholders in the assignments.
///
/// Writes to WorkflowData:
///   - Each key defined in <see cref="_assignments"/>.
///
/// Usage (code-first):
/// <code>
///   new SetVariableNode("Init", new()
///   {
///       ["greeting"] = "Hello, {{user_name}}!",
///       ["max_tokens"] = 500,
///       ["debug"] = true
///   })
/// </code>
/// </summary>
public sealed class SetVariableNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Data";
    public override string Description =>
        $"Sets {_assignments.Count} workflow variable(s)";

    /// <inheritdoc/>
    public override string IdPrefix => "setvar";

    /// <inheritdoc/>
    // Input ports are the {{variable}} placeholders referenced inside assignment values.
    public override IReadOnlyList<NodeData> DataIn => ExtractTemplatePorts();

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
        _assignments.Keys
            .Select(k => new NodeData(k, typeof(object), Description: "Assigned value"))
            .ToList<NodeData>();

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "SetVariableNode",
        Description = "Write literal or {{interpolated}} values into workflow data",
        Parameters  =
        [
            new() { Name = "assignments", Label = "Assignments (JSON)", Type = ParameterType.Json, Required = true,
                Placeholder = "{\"greeting\": \"Hello {{name}}\", \"count\": 0}",
                Description = "Key/value pairs to write. String values support {{variable}} interpolation." },
        ]
    };

    private readonly IReadOnlyDictionary<string, object?> _assignments;

    public SetVariableNode(string name, Dictionary<string, object?> assignments)
    {
        Name         = name;
        _assignments = assignments;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public SetVariableNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Set Variable",
            (NodeParameters.GetStringDict(parameters, "assignments") ?? new Dictionary<string, string>())
                .ToDictionary(kv => kv.Key, kv => (object?)kv.Value))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var output = input.Clone();

        foreach (var (key, value) in _assignments)
        {
            // Interpolate {{variable}} in string values
            var resolved = value is string str ? Interpolate(str, input, nodeCtx) : value;
            output.Set(key, resolved);
            nodeCtx.Log($"Set '{key}' = {resolved}");
        }

        return Task.FromResult(output);
    }

    private static string Interpolate(string template, WorkflowData data, NodeExecutionContext nodeCtx)
    {
        return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
        {
            var key = m.Groups[1].Value;
            if (data.TryGet<object>(key, out var val) && val is not null)
                return val.ToString()!;

            nodeCtx.Log($"⚠️  Template variable '{{{{{key}}}}}' not found");
            return m.Value;
        });
    }

    private IReadOnlyList<NodeData> ExtractTemplatePorts()
    {
        var keys = new HashSet<string>();
        foreach (var (_, value) in _assignments)
        {
            if (value is string str)
                foreach (Match m in Regex.Matches(str, @"\{\{(\w+)\}\}"))
                    keys.Add(m.Groups[1].Value);
        }
        return keys
            .Select(k => new NodeData(k, typeof(object), Required: false,
                Description: $"Template variable {{{{{k}}}}}"))
            .ToList<NodeData>();
    }
}
