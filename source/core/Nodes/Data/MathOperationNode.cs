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
    public override string IdPrefix => "math";

    public override IReadOnlyList<NodeData> DataIn
    {
        get
        {
            var ports = new List<NodeData>();
            if (!string.IsNullOrWhiteSpace(_inputKeyA))
                ports.Add(new(_inputKeyA, typeof(double), Required: false, "First operand"));
            if (!string.IsNullOrWhiteSpace(_inputKeyB))
                ports.Add(new(_inputKeyB, typeof(double), Required: false, "Second operand"));
            return ports;
        }
    }

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_outputKey, typeof(double), Description: "Result of the math operation"),
    ];

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "MathOperationNode",
        Description = "Perform a math operation on one or two numeric values",
        Parameters  =
        [
            new() { Name = "operation",  Label = "Operation", Type = ParameterType.Select, Required = true, DefaultValue = "add",
                Options =
                [
                    new() { Value = "add",       Label = "Add (A + B)" },
                    new() { Value = "subtract",  Label = "Subtract (A − B)" },
                    new() { Value = "multiply",  Label = "Multiply (A × B)" },
                    new() { Value = "divide",    Label = "Divide (A ÷ B)" },
                    new() { Value = "modulo",    Label = "Modulo (A % B)" },
                    new() { Value = "power",     Label = "Power (A ^ B)" },
                    new() { Value = "min",       Label = "Min (A, B)" },
                    new() { Value = "max",       Label = "Max (A, B)" },
                    new() { Value = "abs",       Label = "Absolute Value (|A|)" },
                    new() { Value = "sqrt",      Label = "Square Root (√A)" },
                    new() { Value = "round",     Label = "Round (A)" },
                    new() { Value = "floor",     Label = "Floor (A)" },
                    new() { Value = "ceil",      Label = "Ceiling (A)" },
                    new() { Value = "negate",    Label = "Negate (−A)" },
                ]
            },
            new() { Name = "inputKeyA",  Label = "Operand A — Key",     Type = ParameterType.Text,   Required = false, Placeholder = "count",
                Description = "WorkflowData key for operand A (overrides Operand A Value)" },
            new() { Name = "valueA",     Label = "Operand A — Value",   Type = ParameterType.Number, Required = false, DefaultValue = 0 },
            new() { Name = "inputKeyB",  Label = "Operand B — Key",     Type = ParameterType.Text,   Required = false, Placeholder = "step",
                Description = "WorkflowData key for operand B (overrides Operand B Value)" },
            new() { Name = "valueB",     Label = "Operand B — Value",   Type = ParameterType.Number, Required = false, DefaultValue = 0 },
            new() { Name = "outputKey",  Label = "Output Key",          Type = ParameterType.Text,   Required = true,  Placeholder = "result",
                Description = "WorkflowData key where the result is stored" },
        ]
    };

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
        nodeCtx.SetMetadata("a", a);
        nodeCtx.SetMetadata("b", b);
        nodeCtx.SetMetadata("result", result);

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
