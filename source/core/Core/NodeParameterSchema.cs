namespace Twf.Flow.Core;

/// <summary>
/// Describes the UI schema for a node type: the parameter form fields shown in the
/// properties panel, plus a description used in the node palette tooltip.
/// DataInputs and DataOutputs are populated automatically from INode.DataIn / DataOut
/// by NodeDataMetadataProvider — you do not need to set them manually.
/// </summary>
public class NodeParameterSchema
{
    public string NodeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new();

    // Set by NodeDataMetadataProvider at seed time — not part of the static Schema declaration.
    public List<DataPortInfo> DataInputs  { get; set; } = new();
    public List<DataPortInfo> DataOutputs { get; set; } = new();
}

/// <summary>Describes a WorkflowData key a node reads (input) or writes (output).</summary>
public class DataPortInfo
{
    public string Key         { get; set; } = string.Empty;
    public bool   Required    { get; set; }
    public bool   IsDynamic   { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>Defines a single editable parameter shown in the properties panel.</summary>
public class ParameterDefinition
{
    public string Name         { get; set; } = string.Empty;
    public string Label        { get; set; } = string.Empty;
    public string Description  { get; set; } = string.Empty;
    public ParameterType Type  { get; set; }
    public bool   Required     { get; set; }
    public object? DefaultValue { get; set; }
    public string? Placeholder { get; set; }

    /// <summary>Options list — only used when Type == Select.</summary>
    public List<SelectOption>? Options { get; set; }

    /// <summary>Inclusive lower bound — only used when Type == Number.</summary>
    public double? MinValue { get; set; }

    /// <summary>Inclusive upper bound — only used when Type == Number.</summary>
    public double? MaxValue { get; set; }
}

public class SelectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

/// <summary>Controls how the properties panel renders the input field for a parameter.</summary>
public enum ParameterType
{
    Text,
    TextArea,
    Number,
    Boolean,
    Select,
    Json,
    Color,
}
