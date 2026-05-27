using Twf.Flow.Core;

namespace Twf.Flow.Nodes.IO;

// ═══════════════════════════════════════════════════════════════════════════════
// FileReaderNode — Read files from disk
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Reads a file from the filesystem and puts its content into WorkflowData.
/// Supports plain text, JSON, and CSV files. PDF support requires a PDF library.
///
/// Reads from WorkflowData:
///   - "file_path" : path to the file (overrides static config)
///
/// Writes to WorkflowData:
///   - "text"          : file content as string
///   - "file_name"     : filename without path
///   - "file_size"     : file size in bytes
///   - "file_extension": e.g. "txt", "json", "csv"
/// </summary>
public sealed class FileReaderNode : BaseNode
{
    public override string Name => "FileReaderNode";
    public override string Category => "IO";
    public override string Description => "Reads a file from disk and writes content and metadata to workflow data";

    // WorkflowData keys
    public const string InputFilePath      = "file_path";
    public const string OutputText         = "text";
    public const string OutputFileName     = "file_name";
    public const string OutputFileSize     = "file_size";
    public const string OutputFileExtension = "file_extension";

    private readonly string? _staticFilePath;

    public FileReaderNode(string? staticFilePath = null)
    {
        _staticFilePath = staticFilePath;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public FileReaderNode(Dictionary<string, object?> parameters)
        : this(NodeParameters.GetString(parameters, "filePath"))
    { }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var filePath = input.GetString(InputFilePath) ?? _staticFilePath
            ?? throw new InvalidOperationException(
                $"FileReaderNode requires '{InputFilePath}' in WorkflowData or static config");

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var info = new FileInfo(filePath);
        nodeCtx.Log($"Reading file: {info.Name} ({info.Length} bytes)");

        var content = await File.ReadAllTextAsync(filePath, context.CancellationToken);
        var ext = info.Extension.TrimStart('.').ToLower();

        nodeCtx.SetMetadata("file_size", info.Length);
        nodeCtx.SetMetadata("file_extension", ext);

        return input.Clone()
            .Set(OutputText,          content)
            .Set(OutputFileName,      info.Name)
            .Set(OutputFileSize,      info.Length)
            .Set(OutputFileExtension, ext);
    }
}
