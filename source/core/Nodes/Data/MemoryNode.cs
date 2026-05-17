using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Reads from or writes to the workflow's global state memory.
/// Use for maintaining context across pipeline runs — user preferences,
/// accumulated results, session state, etc.
/// </summary>
public sealed class MemoryNode : BaseNode
{
    public override string Name => $"Memory:{_mode}";
    public override string Category => "Data";
    public override string Description =>
        _mode == MemoryMode.Read
            ? $"Reads {string.Join(", ", _keys)} from global memory"
            : $"Writes {string.Join(", ", _keys)} to global memory";

    /// <inheritdoc/>
    public override string IdPrefix => "memory";

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataIn =>
        _mode == MemoryMode.Write
            ? _keys.Select(k => new NodeData(k, typeof(object), Required: false, "Written to global memory")).ToList<NodeData>()
            : [];

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
        _mode == MemoryMode.Read
            ? _keys.Select(k => new NodeData(k, typeof(object), Required: false, "Read from global memory")).ToList<NodeData>()
            : [];

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "MemoryNode",
        Description = "Read or write keys from persistent workflow memory (state)",
        Parameters  =
        [
            new() { Name = "mode", Label = "Mode", Type = ParameterType.Select, Required = true, DefaultValue = "read",
                Options =
                [
                    new() { Value = "read",  Label = "Read from Memory" },
                    new() { Value = "write", Label = "Write to Memory" },
                ] },
            new() { Name = "keys", Label = "Keys (JSON array)", Type = ParameterType.Json, Required = true,
                Placeholder = "[\"user_id\", \"session_state\"]" },
        ]
    };

    private readonly MemoryMode _mode;
    private readonly string[] _keys;

    private MemoryNode(MemoryMode mode, string[] keys)
    {
        _mode = mode;
        _keys = keys;
    }

    /// <summary>Dictionary constructor for dynamic instantiation. Parameters: mode ("read"/"write"), keys (string array).</summary>
    public MemoryNode(Dictionary<string, object?> parameters)
    {
        var mode = NodeParameters.GetString(parameters, "mode") ?? "read";
        _mode = mode.Equals("write", StringComparison.OrdinalIgnoreCase)
            ? MemoryMode.Write : MemoryMode.Read;
        _keys = (NodeParameters.GetStringList(parameters, "keys") ?? []).ToArray();
    }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var output = input.Clone();

        if (_mode == MemoryMode.Read)
        {
            foreach (var key in _keys)
            {
                var val = context.GetState<object>(key);
                if (val is not null)
                {
                    output.Set(key, val);
                    nodeCtx.Log($"Read '{key}' from memory");
                }
                else
                {
                    nodeCtx.Log($"⚠️  Key '{key}' not found in memory");
                }
            }
        }
        else // Write
        {
            foreach (var key in _keys)
            {
                var val = input.Get<object>(key);
                if (val is not null)
                {
                    context.SetState(key, val);
                    nodeCtx.Log($"Wrote '{key}' to memory");
                }
            }
        }

        return Task.FromResult(output);
    }

    public static MemoryNode Read(params string[] keys) => new(MemoryMode.Read, keys);
    public static MemoryNode Write(params string[] keys) => new(MemoryMode.Write, keys);
}