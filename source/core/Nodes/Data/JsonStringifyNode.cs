using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Serializes a WorkflowData value to a JSON string.
/// Handles dictionaries, lists, primitives, JsonElement, and POCOs.
///
/// Reads from WorkflowData:
///   - <see cref="_inputKey"/>: the value to serialize
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: JSON string representation
/// </summary>
public sealed class JsonStringifyNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"Serializes '{_inputKey}' to JSON → '{_outputKey}'";

    // WorkflowData keys
    public const string DefaultInputKey  = "my_object";
    public const string DefaultOutputKey = "json_string";

    private readonly string _inputKey;
    private readonly string _outputKey;
    private readonly bool   _is_indented;

    private static readonly JsonSerializerOptions _compact  = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions _indented = new() { WriteIndented = true  };

    public JsonStringifyNode(string name, string inputKey, string outputKey = DefaultOutputKey, bool indented = false)
    {
        Name       = name;
        _inputKey  = inputKey;
        _outputKey = outputKey;
        _is_indented  = indented;
    }

    public JsonStringifyNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "JSON Stringify",
            NodeParameters.GetString(parameters, "inputKey")  ?? DefaultInputKey,
            NodeParameters.GetString(parameters, "outputKey") ?? DefaultOutputKey,
            NodeParameters.GetBool(parameters, "indented"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var value   = input.Get<object>(_inputKey);
        var options = _is_indented ? _indented : _compact;
        var json    = JsonSerializer.Serialize(value, options);

        var output = input.Clone().Set(_outputKey, json);

        nodeCtx.Log($"Serialized '{_inputKey}' → {json.Length} chars{(_is_indented ? " (pretty)" : "")}");
        nodeCtx.SetMetadata("json_length", json.Length);

        return Task.FromResult(output);
    }
}
