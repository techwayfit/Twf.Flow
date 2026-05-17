using Twf.Flow.Core;

namespace Twf.Flow.Nodes.IO;

// ═══════════════════════════════════════════════════════════════════════════════
// FileWriterNode — Write output to a file
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Writes WorkflowData content to a file.
/// </summary>
public sealed class FileWriterNode : BaseNode
{
    public override string Name => Schema.NodeType;
    public override string Category => "IO";
    public override string Description => Schema.Description;

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "FileWriterNode",
        Description = "Write a workflow data key to a local file",
        Parameters  =
        [
            new() { Name = "filePath",   Label = "File Path",   Type = ParameterType.Text, Required = true,
                Placeholder = "/data/output/{{request_id}}.txt",
                Description = "Supports {{variable}} substitution" },
            new() { Name = "contentKey", Label = "Content Key", Type = ParameterType.Text, Required = true,
                Placeholder = "e.g. llm_response",
                Description = "WorkflowData key whose value is written to the file" },
        ]
    };

    // WorkflowData keys
    public const string DefaultDataKey = "llm_response";
    public const string OutputFile     = "output_file";

    private readonly string _outputPath;
    private readonly string _dataKey;

    public FileWriterNode(string outputPath, string dataKey = DefaultDataKey)
    {
        _outputPath = outputPath;
        _dataKey = dataKey;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public FileWriterNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "filePath") ?? "output.txt",
            NodeParameters.GetString(parameters, "contentKey") ?? DefaultDataKey)
    { }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var content = input.Get<object>(_dataKey)?.ToString()
            ?? throw new InvalidOperationException(
                $"FileWriterNode: Key '{_dataKey}' not found in WorkflowData");

        var dir = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(_outputPath, content, context.CancellationToken);

        nodeCtx.Log($"Wrote {content.Length} chars to {_outputPath}");
        nodeCtx.SetMetadata("output_path", _outputPath);
        nodeCtx.SetMetadata("bytes_written", content.Length);

        return input.Clone().Set(OutputFile, _outputPath);
    }
}
