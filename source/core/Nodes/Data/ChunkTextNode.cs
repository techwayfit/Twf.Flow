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
    public override string Name => Schema.NodeType;
    public override string Category => "Data";
    public override string Description =>
        $"Splits text into {_config.ChunkSize}-char chunks with {_config.Overlap}-char overlap";

    /// <inheritdoc/>
    public override string IdPrefix => "chunk";

    // WorkflowData keys
    public const string InputText    = "text";
    public const string InputSource  = "source";
    public const string OutputChunks     = "chunks";
    public const string OutputChunkCount = "chunk_count";

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(InputText,   typeof(string), Required: true,  "Source text to split"),
        new(InputSource, typeof(string), Required: false, "Label attached to each chunk for provenance")
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(OutputChunks,     typeof(List<TextChunk>), Description: "List of text chunks"),
        new(OutputChunkCount, typeof(int),             Description: "Number of chunks produced")
    ];

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType = "ChunkTextNode",
        Description = "Split text into overlapping chunks (character/word/sentence)",
        Parameters =
        [
            new() { Name = "chunkSize", Label = "Chunk Size",          Type = ParameterType.Number, Required = false, DefaultValue = 500,  MinValue = 50, MaxValue = 10000 },
            new() { Name = "overlap",   Label = "Overlap",             Type = ParameterType.Number, Required = false, DefaultValue = 50,   MinValue = 0,  MaxValue = 1000 },
            new() { Name = "strategy",  Label = "Chunking Strategy",   Type = ParameterType.Select, Required = false, DefaultValue = "Character",
                Options =
                [
                    new() { Value = "Character", Label = "By Character" },
                    new() { Value = "Word",      Label = "By Word" },
                    new() { Value = "Sentence",  Label = "By Sentence" },
                ] },
        ]
    };

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
        nodeCtx.SetMetadata("chunk_count", chunks.Count);
        nodeCtx.SetMetadata("avg_chunk_size",
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