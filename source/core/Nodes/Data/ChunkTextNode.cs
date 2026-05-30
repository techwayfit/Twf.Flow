using Twf.Flow.Core;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes;
using System.Text.RegularExpressions;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Splits large text into overlapping chunks suitable for embedding and RAG.
/// Supports character-based, word-based, and sentence-based chunking strategies.
///
/// Reads from WorkflowData:
///   - "text" : the source text to chunk
///
/// Writes to WorkflowData:
///   - "chunks"      : List&lt;TextChunk&gt; — the chunked result
///   - "chunk_count" : number of chunks created
/// </summary>
public sealed class ChunkTextNode : BaseNode
{
    public override string Name => "ChunkTextNode";
    public override string Category => "Data";
    public override string Description =>
        $"Splits text into {_config.ChunkSize}-char chunks with {_config.Overlap}-char overlap";

    /// <inheritdoc/>

    // WorkflowData keys
    public const string InputText    = "text";
    public const string InputSource  = "source";
    public const string OutputChunks     = "chunks";
    public const string OutputChunkCount = "chunk_count";

    /// <inheritdoc/>

    /// <inheritdoc/>

    private readonly ChunkConfig _config;

    public ChunkTextNode(ChunkConfig? config = null)
    {
        _config = config ?? new ChunkConfig();
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public ChunkTextNode(Dictionary<string, object?> parameters)
        : this(new ChunkConfig
        {
            ChunkSize = ChunkSize.FromValue(NodeParameters.GetInt(parameters, "chunkSize", 500)),
            Overlap = ChunkOverlap.FromValue(NodeParameters.GetInt(parameters, "overlap", 50)),
            Strategy = Enum.TryParse<ChunkStrategy>(
                NodeParameters.GetString(parameters, "strategy"), true, out var strat)
                ? strat : ChunkStrategy.Character
        })
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var text = input.GetRequiredString(InputText);
        var source = input.GetString(InputSource) ?? "unknown";

        var chunks = _config.Strategy switch
        {
            ChunkStrategy.Character => ChunkByCharacter(text, source),
            ChunkStrategy.Word => ChunkByWord(text, source),
            ChunkStrategy.Sentence => ChunkBySentence(text, source),
            _ => ChunkByCharacter(text, source)
        };

        nodeCtx.Log($"Split {text.Length} chars into {chunks.Count} chunks " +
                    $"(strategy={_config.Strategy}, size={_config.ChunkSize})");
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.ChunkCount, chunks.Count);
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Data.AvgChunkSize,
            chunks.Count > 0 ? chunks.Average(c => c.Text.Length) : 0);

        return Task.FromResult(input.Clone()
            .Set(OutputChunks,     chunks)
            .Set(OutputChunkCount, chunks.Count));
    }

    private List<TextChunk> ChunkByCharacter(string text, string source)
    {
        var chunks = new List<TextChunk>();
        var i = 0;
        while (i < text.Length)
        {
            var end = Math.Min(i + _config.ChunkSize, text.Length);
            chunks.Add(new TextChunk(
                text[i..end], source, chunks.Count, i, end));
            i += _config.ChunkSize - _config.Overlap;
        }
        return chunks;
    }

    private List<TextChunk> ChunkByWord(string text, string source)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<TextChunk>();
        var i = 0;
        while (i < words.Length)
        {
            var batch = words.Skip(i).Take(_config.ChunkSize).ToArray();
            chunks.Add(new TextChunk(
                string.Join(" ", batch), source, chunks.Count, i, i + batch.Length));
            i += _config.ChunkSize - _config.Overlap;
        }
        return chunks;
    }

    private List<TextChunk> ChunkBySentence(string text, string source)
    {
        var sentences = text.Split(new[] { ". ", "! ", "? " },
            StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<TextChunk>();
        var i = 0;
        while (i < sentences.Length)
        {
            var batch = sentences.Skip(i).Take(_config.ChunkSize);
            chunks.Add(new TextChunk(
                string.Join(". ", batch) + ".", source, chunks.Count, i, i + _config.ChunkSize));
            i += _config.ChunkSize - _config.Overlap;
        }
        return chunks;
    }
}