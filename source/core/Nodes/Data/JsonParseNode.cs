using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Parses a JSON string stored in WorkflowData into a structured object.
/// The parsed result is stored back under <see cref="_outputKey"/> as a
/// <see cref="JsonElement"/> (or strongly typed via target type when specified).
///
/// Reads from WorkflowData:
///   - <see cref="_inputKey"/>: string containing JSON
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: deserialized JSON object/array
///   - json_parse_success: true/false
/// </summary>
public sealed class JsonParseNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Parses JSON from '{_inputKey}' → '{_outputKey}'";
    public override string IdPrefix => "jsonparse";

    // WorkflowData keys
    public const string DefaultInputKey    = "json_string";
    public const string DefaultOutputKey   = "parsed_json";
    public const string OutputParseSuccess = "json_parse_success";

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(_inputKey, typeof(string), Required: true, "JSON string to parse"),
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_outputKey,        typeof(object), Description: "Parsed JSON value"),
        new(OutputParseSuccess, typeof(bool),   Description: "Whether parsing succeeded"),
    ];

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "JsonParseNode",
        Description = "Parse a JSON string into a structured object",
        Parameters  =
        [
            new() { Name = "inputKey",   Label = "Input Key",  Type = ParameterType.Text, Required = true,  Placeholder = "json_string",
                Description = "WorkflowData key holding the JSON string" },
            new() { Name = "outputKey",  Label = "Output Key", Type = ParameterType.Text, Required = false, DefaultValue = "parsed_json" },
            new() { Name = "strict",     Label = "Strict Mode", Type = ParameterType.Boolean, Required = false, DefaultValue = false,
                Description = "If true, throws on parse failure instead of setting json_parse_success=false" },
        ]
    };

    private readonly string _inputKey;
    private readonly string _outputKey;
    private readonly bool   _strict;

    public JsonParseNode(string name, string inputKey, string outputKey = DefaultOutputKey, bool strict = false)
    {
        Name       = name;
        _inputKey  = inputKey;
        _outputKey = outputKey;
        _strict    = strict;
    }

    public JsonParseNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "JSON Parse",
            NodeParameters.GetString(parameters, "inputKey")  ?? DefaultInputKey,
            NodeParameters.GetString(parameters, "outputKey") ?? DefaultOutputKey,
            NodeParameters.GetBool(parameters, "strict"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var jsonStr = input.GetString(_inputKey) ?? string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var parsed = doc.RootElement.Clone();  // clone so doc can be disposed

            // Flatten to a more usable form
            object result = parsed.ValueKind switch
            {
                JsonValueKind.Object => parsed.EnumerateObject()
                    .ToDictionary(p => p.Name, p => (object?)UnboxElement(p.Value),
                        StringComparer.OrdinalIgnoreCase),
                JsonValueKind.Array  => parsed.EnumerateArray()
                    .Select(e => (object?)UnboxElement(e)).ToList(),
                JsonValueKind.String => parsed.GetString()!,
                JsonValueKind.Number => parsed.TryGetInt64(out var l) ? (object)l : parsed.GetDouble(),
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                _                   => (object?)null,
            };

            var output = input.Clone()
                .Set(_outputKey,        result)
                .Set(OutputParseSuccess, true);

            nodeCtx.Log($"Parsed JSON ({jsonStr.Length} chars) → {parsed.ValueKind}");
            nodeCtx.SetMetadata("value_kind",  parsed.ValueKind.ToString());
            nodeCtx.SetMetadata("input_length", jsonStr.Length);

            return Task.FromResult(output);
        }
        catch (JsonException ex)
        {
            if (_strict)
                throw new InvalidOperationException($"[{Name}] JSON parse failed: {ex.Message}", ex);

            nodeCtx.Log($"⚠️ JSON parse failed: {ex.Message}");
            var errOutput = input.Clone()
                .Set(_outputKey,        (object?)null)
                .Set(OutputParseSuccess, false);

            return Task.FromResult(errOutput);
        }
    }

    // Unbox a JsonElement recursively to plain .NET types
    private static object? UnboxElement(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object  => e.EnumerateObject()
            .ToDictionary(p => p.Name, p => UnboxElement(p.Value),
                StringComparer.OrdinalIgnoreCase),
        JsonValueKind.Array   => e.EnumerateArray().Select(UnboxElement).ToList(),
        JsonValueKind.String  => e.GetString(),
        JsonValueKind.Number  => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        _                     => null,
    };
}
