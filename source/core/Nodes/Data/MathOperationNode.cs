using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Performs a mathematical operation on one or two numeric operands.
/// Operands can be literal values or WorkflowData key references.
///
/// Supported operations: add, subtract, multiply, divide, modulo, power,
///   abs, round, floor, ceil, min, max, sqrt, negate.
///
/// Reads from WorkflowData:
///   - <see cref="_inputKeyA"/>: (optional) first operand
///   - <see cref="_inputKeyB"/>: (optional) second operand
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: numeric result
/// </summary>
public sealed class MathOperationNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "Data";
    public override string Description => $"{_operation}({_inputKeyA ?? _valueA.ToString()}, {_inputKeyB ?? _valueB?.ToString() ?? "–"}) → {_outputKey}";

    private readonly string  _operation;
    private readonly string? _inputKeyA;
    private readonly double  _valueA;
    private readonly string? _inputKeyB;
    private readonly double? _valueB;
    private readonly string  _outputKey;

    public MathOperationNode(string name, string operation, string outputKey,
        string? inputKeyA = null, double valueA = 0,
        string? inputKeyB = null, double? valueB = null)
    {
        Name       = name;
        _operation = operation;
        _outputKey = outputKey;
        _inputKeyA = inputKeyA;
        _valueA    = valueA;
        _inputKeyB = inputKeyB;
        _valueB    = valueB;
    }

    public MathOperationNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "Math Operation",
            NodeParameters.GetString(parameters, "operation") ?? "add",
            NodeParameters.GetString(parameters, "outputKey") ?? "result",
            NodeParameters.GetString(parameters, "inputKeyA"),
            NodeParameters.GetDouble(parameters, "valueA"),
            NodeParameters.GetString(parameters, "inputKeyB"),
            NodeParameters.GetDouble(parameters, "valueB"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var a = ResolveOperand(input, _inputKeyA, _valueA);
        var b = ResolveOperand(input, _inputKeyB, _valueB ?? 0);

        double result = _operation switch
        {
            "add"      => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide"   => b == 0 ? throw new DivideByZeroException($"[{Name}] Division by zero") : a / b,
            "modulo"   => b == 0 ? throw new DivideByZeroException($"[{Name}] Modulo by zero")   : a % b,
            "power"    => Math.Pow(a, b),
            "min"      => Math.Min(a, b),
            "max"      => Math.Max(a, b),
            "abs"      => Math.Abs(a),
            "sqrt"     => Math.Sqrt(a),
            "round"    => Math.Round(a),
            "floor"    => Math.Floor(a),
            "ceil"     => Math.Ceiling(a),
            "negate"   => -a,
            _          => throw new InvalidOperationException($"Unknown operation '{_operation}'"),
        };

        var output = input.Clone().Set(_outputKey, result);

        nodeCtx.Log($"{_operation}({a}, {b}) = {result}");
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.A, a);
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.B, b);
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.Result, result);

        return Task.FromResult(output);
    }

    private static double ResolveOperand(WorkflowData data, string? key, double fallback)
    {
        if (string.IsNullOrWhiteSpace(key) || !data.Has(key)) return fallback;
        var raw = data.Get<object>(key);
        if (raw is null) return fallback;
        return double.TryParse(raw.ToString(), out var d) ? d : fallback;
    }
}
