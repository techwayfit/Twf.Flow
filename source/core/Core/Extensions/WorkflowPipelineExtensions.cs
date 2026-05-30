using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Data;
using Twf.Flow.Nodes.IO;

namespace Twf.Flow.Core.Extensions;

// ─── Config Types ─────────────────────────────────────────────────────────────

/// <summary>
/// Configuration for <see cref="WorkflowPipelineExtensions.AddAIPipeline"/>.
/// Bundles PromptBuilderNode → LlmNode → (optional) OutputParserNode.
///
/// After execution the LLM response is in WorkflowData under <see cref="LlmNode.OutputResponse"/>.
/// If <see cref="ParseJsonOutput"/> is true, parsed fields are also written (see <see cref="LlmNode.OutputResponse"/>
/// and <see cref="OutputParserNode.OutputParsedOutput"/>).
/// </summary>
public sealed class AIPipelineConfig
{
    /// <summary>LLM provider, model, and API key settings.</summary>
    public required LlmConfig Llm { get; init; }

    /// <summary>Prompt template with <c>{{variable}}</c> slots (e.g. "Summarise: {{text}}").</summary>
    public required string PromptTemplate { get; init; }

    /// <summary>Optional system prompt template. Supports <c>{{variable}}</c> slots.</summary>
    public string? SystemTemplate { get; init; }

    /// <summary>
    /// Static variables injected into the prompt template at build time.
    /// Useful for constants that don't come from WorkflowData.
    /// </summary>
    public Dictionary<string, object?>? StaticVariables { get; init; }

    /// <summary>
    /// When true, appends an <see cref="OutputParserNode"/> that extracts structured JSON
    /// from the LLM response. Parsed fields land in WorkflowData alongside
    /// <see cref="OutputParserNode.OutputParsedOutput"/>.
    /// </summary>
    public bool ParseJsonOutput { get; init; } = false;

    /// <summary>
    /// Optional field mapping passed to <see cref="OutputParserNode"/> when
    /// <see cref="ParseJsonOutput"/> is true. Maps JSON keys → WorkflowData keys.
    /// If null, all JSON keys are written directly.
    /// </summary>
    public Dictionary<string, string>? FieldMapping { get; init; }

    /// <summary>
    /// When true the OutputParserNode throws if valid JSON cannot be extracted.
    /// Ignored when <see cref="ParseJsonOutput"/> is false.
    /// </summary>
    public bool StrictParsing { get; init; } = false;

    /// <summary>Prefix applied to generated node names (default: "AI").</summary>
    public string NodePrefix { get; init; } = "AI";
}

/// <summary>
/// Configuration for <see cref="WorkflowPipelineExtensions.AddEmbeddingPipeline"/>.
/// Bundles an optional <see cref="ChunkTextNode"/> → <see cref="EmbeddingNode"/>.
///
/// Output lands in <see cref="EmbeddingNode.OutputEmbedding"/> (single text) or
/// <see cref="EmbeddingNode.OutputEmbeddings"/> (batch / after chunking).
/// </summary>
public sealed class EmbeddingPipelineConfig
{
    /// <summary>Embedding model and API key settings.</summary>
    public required EmbeddingConfig Embedding { get; init; }

    /// <summary>
    /// When set, a <see cref="ChunkTextNode"/> is prepended to split the source text
    /// before embedding. The chunks are then embedded as a batch.
    /// Reads from <see cref="ChunkTextNode.InputText"/> and writes
    /// <see cref="ChunkTextNode.OutputChunks"/> + <see cref="EmbeddingNode.OutputEmbeddings"/>.
    /// </summary>
    public ChunkConfig? Chunk { get; init; }

    /// <summary>Prefix applied to generated node names (default: "Embed").</summary>
    public string NodePrefix { get; init; } = "Embed";
}

/// <summary>
/// Configuration for <see cref="WorkflowPipelineExtensions.AddSearchAndSummarizePipeline"/>.
/// Bundles <see cref="GoogleSearchNode"/> → <see cref="PromptBuilderNode"/> → <see cref="LlmNode"/>.
///
/// The search query is read from <see cref="GoogleSearchNode.InputQuery"/>.
/// The final summary lands in <see cref="LlmNode.OutputResponse"/>.
/// </summary>
public sealed class SearchAndSummarizeConfig
{
    /// <summary>SerpApi API key for Google search.</summary>
    public required string SearchApiKey { get; init; }

    /// <summary>LLM config used to summarise the search results.</summary>
    public required LlmConfig Llm { get; init; }

    /// <summary>
    /// Prompt template that formats search results into a prompt.
    /// The template receives <c>{{search_results}}</c> (newline-joined snippets)
    /// and <c>{{search_query}}</c> as variables.
    /// Defaults to a sensible summarization prompt if not set.
    /// </summary>
    public string? SummarizeTemplate { get; init; }

    /// <summary>Prefix applied to generated node names (default: "Search").</summary>
    public string NodePrefix { get; init; } = "Search";
}

// ─── Extension Methods ────────────────────────────────────────────────────────

/// <summary>
/// Fluent extension methods on <see cref="WorkflowBuilder"/> that add multi-node pipeline
/// sequences in a single call.
/// </summary>
public static class WorkflowPipelineExtensions
{
    // ── AI Pipeline ───────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a complete AI inference pipeline:
    /// <see cref="PromptBuilderNode"/> → <see cref="LlmNode"/> → (optional) <see cref="OutputParserNode"/>.
    ///
    /// <list type="bullet">
    ///   <item>Reads: any <c>{{variable}}</c> keys referenced in <see cref="AIPipelineConfig.PromptTemplate"/></item>
    ///   <item>Writes: <see cref="LlmNode.OutputResponse"/>, <see cref="LlmNode.OutputModel"/>,
    ///     <see cref="LlmNode.OutputPromptTokens"/>, <see cref="LlmNode.OutputCompletionTokens"/></item>
    ///   <item>Also writes <see cref="OutputParserNode.OutputParsedOutput"/> when
    ///     <see cref="AIPipelineConfig.ParseJsonOutput"/> is true</item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await WorkflowBuilder.Create("Summarizer")
    ///     .AddAIPipeline(new AIPipelineConfig
    ///     {
    ///         Llm            = LlmConfig.OpenAI(apiKey, "gpt-4o"),
    ///         PromptTemplate = "Summarise this in 3 bullet points:\n\n{{text}}",
    ///         SystemTemplate = "You are a concise technical writer.",
    ///     })
    ///     .RunAsync(new WorkflowData().Set("text", articleText));
    ///
    /// var summary = result.Data.GetString(LlmNode.OutputResponse);
    /// </code>
    /// </example>
    public static WorkflowBuilder AddAIPipeline(this WorkflowBuilder workflow, AIPipelineConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        workflow
            .AddNode(new PromptBuilderNode(
                $"{config.NodePrefix}:PromptBuilder",
                config.PromptTemplate,
                config.SystemTemplate,
                config.StaticVariables))
            .AddNode(new LlmNode(
                $"{config.NodePrefix}:LLM",
                config.Llm));

        if (config.ParseJsonOutput)
            workflow.AddNode(new OutputParserNode(
                $"{config.NodePrefix}:OutputParser",
                config.FieldMapping,
                config.StrictParsing));

        return workflow;
    }

    // ── Embedding Pipeline ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a vector embedding pipeline:
    /// (optional) <see cref="ChunkTextNode"/> → <see cref="EmbeddingNode"/>.
    ///
    /// <list type="bullet">
    ///   <item>Without chunking: reads <see cref="EmbeddingNode.InputText"/>,
    ///     writes <see cref="EmbeddingNode.OutputEmbedding"/></item>
    ///   <item>With chunking: reads <see cref="ChunkTextNode.InputText"/>,
    ///     writes <see cref="ChunkTextNode.OutputChunks"/> and
    ///     <see cref="EmbeddingNode.OutputEmbeddings"/></item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// // Simple — embed a single text
    /// var result = await WorkflowBuilder.Create("Embed")
    ///     .AddEmbeddingPipeline(new EmbeddingPipelineConfig
    ///     {
    ///         Embedding = EmbeddingConfig.OpenAI(apiKey),
    ///     })
    ///     .RunAsync(new WorkflowData().Set("text", "Hello world"));
    ///
    /// var vector = result.Data.Get&lt;float[]&gt;(EmbeddingNode.OutputEmbedding);
    ///
    /// // With chunking — embed a long document as a batch
    /// var result = await WorkflowBuilder.Create("ChunkAndEmbed")
    ///     .AddEmbeddingPipeline(new EmbeddingPipelineConfig
    ///     {
    ///         Embedding = EmbeddingConfig.OpenAI(apiKey),
    ///         Chunk     = new ChunkConfig { ChunkSize = ChunkSize.FromValue(500) },
    ///     })
    ///     .RunAsync(new WorkflowData().Set("text", longDocument));
    ///
    /// var embeddings = result.Data.Get&lt;List&lt;float[]&gt;&gt;(EmbeddingNode.OutputEmbeddings);
    /// </code>
    /// </example>
    public static WorkflowBuilder AddEmbeddingPipeline(this WorkflowBuilder workflow, EmbeddingPipelineConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Chunk is not null)
        {
            // Chunk the text first, then embed each chunk as a batch.
            // An inline step bridges the gap: extracts chunk texts into "texts" for EmbeddingNode.
            workflow
                .AddNode(new ChunkTextNode(config.Chunk))
                .AddStep($"{config.NodePrefix}:ExtractChunkTexts",
                    (data, _) =>
                    {
                        var chunks = data.Get<List<TextChunk>>(ChunkTextNode.OutputChunks);
                        var texts  = chunks?.Select(c => c.Text).ToList() ?? [];
                        return Task.FromResult(data.Clone().Set(EmbeddingNode.InputTexts, texts));
                    });
        }

        workflow.AddNode(new EmbeddingNode($"{config.NodePrefix}:Embedding", config.Embedding));
        return workflow;
    }

    // ── Search + Summarize Pipeline ───────────────────────────────────────────

    /// <summary>
    /// Adds a search-and-summarize pipeline:
    /// <see cref="GoogleSearchNode"/> → <see cref="PromptBuilderNode"/> → <see cref="LlmNode"/>.
    ///
    /// <list type="bullet">
    ///   <item>Reads: <see cref="GoogleSearchNode.InputQuery"/> ("search_query")</item>
    ///   <item>Writes: <see cref="GoogleSearchNode.OutputResults"/>, <see cref="LlmNode.OutputResponse"/></item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await WorkflowBuilder.Create("ResearchBot")
    ///     .AddSearchAndSummarizePipeline(new SearchAndSummarizeConfig
    ///     {
    ///         SearchApiKey = serpApiKey,
    ///         Llm          = LlmConfig.OpenAI(openAiKey, "gpt-4o"),
    ///     })
    ///     .RunAsync(new WorkflowData().Set("search_query", "latest AI breakthroughs 2025"));
    ///
    /// var summary = result.Data.GetString(LlmNode.OutputResponse);
    /// </code>
    /// </example>
    public static WorkflowBuilder AddSearchAndSummarizePipeline(
        this WorkflowBuilder workflow, SearchAndSummarizeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        const string DefaultSummarizeTemplate =
            "Search query: {{search_query}}\n\n" +
            "Top results:\n{{search_results}}\n\n" +
            "Write a concise, factual summary of the above results.";

        var summarizeTemplate = config.SummarizeTemplate ?? DefaultSummarizeTemplate;

        workflow
            .AddNode(new GoogleSearchNode(config.SearchApiKey))
            // Bridge: flatten search results into a "search_results" string for the prompt template
            .AddStep($"{config.NodePrefix}:FormatResults",
                (data, _) =>
                {
                    var results = data.Get<List<SearchResultItem>>(GoogleSearchNode.OutputResults) ?? [];
                    var formatted = string.Join("\n\n", results.Select((r, i) =>
                        $"{i + 1}. {r.Title}\n{r.Description}\n{r.LinkedPage}"));
                    return Task.FromResult(data.Clone().Set(WorkflowDataKeys.Pipelines.SearchResults, formatted));
                })
            .AddNode(new PromptBuilderNode(
                $"{config.NodePrefix}:PromptBuilder",
                summarizeTemplate))
            .AddNode(new LlmNode(
                $"{config.NodePrefix}:LLM",
                config.Llm));

        return workflow;
    }
}
