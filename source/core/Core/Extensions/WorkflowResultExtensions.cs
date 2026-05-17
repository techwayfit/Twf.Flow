using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Data;
using Twf.Flow.Nodes.IO;
using Twf.Flow.Core.ValueObjects;

namespace Twf.Flow.Core.Extensions;

/// <summary>
/// Typed accessors for well-known WorkflowData keys on <see cref="WorkflowResult"/>
/// and <see cref="WorkflowData"/>.
///
/// Instead of:
/// <code>result.Data.GetString(LlmNode.OutputResponse)</code>
/// write:
/// <code>result.LlmResponse()</code>
/// </summary>
public static class WorkflowResultExtensions
{
    // ── AI / LLM ─────────────────────────────────────────────────────────────

    /// <summary>Returns the LLM text response (<see cref="LlmNode.OutputResponse"/>).</summary>
    public static string? LlmResponse(this WorkflowResult result) =>
        result.Data.GetString(LlmNode.OutputResponse);

    /// <inheritdoc cref="LlmResponse(WorkflowResult)"/>
    public static string? LlmResponse(this WorkflowData data) =>
        data.GetString(LlmNode.OutputResponse);

    /// <summary>Returns the model name used (<see cref="LlmNode.OutputModel"/>).</summary>
    public static string? LlmModel(this WorkflowResult result) =>
        result.Data.GetString(LlmNode.OutputModel);

    /// <inheritdoc cref="LlmModel(WorkflowResult)"/>
    public static string? LlmModel(this WorkflowData data) =>
        data.GetString(LlmNode.OutputModel);

    /// <summary>Returns prompt token count (<see cref="LlmNode.OutputPromptTokens"/>).</summary>
    public static int PromptTokens(this WorkflowResult result) =>
        result.Data.Get<int>(LlmNode.OutputPromptTokens);

    /// <summary>Returns completion token count (<see cref="LlmNode.OutputCompletionTokens"/>).</summary>
    public static int CompletionTokens(this WorkflowResult result) =>
        result.Data.Get<int>(LlmNode.OutputCompletionTokens);

    // ── Output Parser ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full parsed JSON object (<see cref="OutputParserNode.OutputParsedOutput"/>).
    /// Only populated when <see cref="AIPipelineConfig.ParseJsonOutput"/> is true.
    /// </summary>
    public static Dictionary<string, object?>? ParsedOutput(this WorkflowResult result) =>
        result.Data.Get<Dictionary<string, object?>>(OutputParserNode.OutputParsedOutput);

    /// <inheritdoc cref="ParsedOutput(WorkflowResult)"/>
    public static Dictionary<string, object?>? ParsedOutput(this WorkflowData data) =>
        data.Get<Dictionary<string, object?>>(OutputParserNode.OutputParsedOutput);

    // ── Embedding ─────────────────────────────────────────────────────────────

    /// <summary>Returns the single embedding vector (<see cref="EmbeddingNode.OutputEmbedding"/>).</summary>
    public static float[]? Embedding(this WorkflowResult result) =>
        result.Data.Get<float[]>(EmbeddingNode.OutputEmbedding);

    /// <inheritdoc cref="Embedding(WorkflowResult)"/>
    public static float[]? Embedding(this WorkflowData data) =>
        data.Get<float[]>(EmbeddingNode.OutputEmbedding);

    /// <summary>Returns batch embedding vectors (<see cref="EmbeddingNode.OutputEmbeddings"/>).</summary>
    public static List<float[]>? Embeddings(this WorkflowResult result) =>
        result.Data.Get<List<float[]>>(EmbeddingNode.OutputEmbeddings);

    /// <inheritdoc cref="Embeddings(WorkflowResult)"/>
    public static List<float[]>? Embeddings(this WorkflowData data) =>
        data.Get<List<float[]>>(EmbeddingNode.OutputEmbeddings);

    // ── Google Search ─────────────────────────────────────────────────────────

    /// <summary>Returns organic search results (<see cref="GoogleSearchNode.OutputResults"/>).</summary>
    public static List<SearchResultItem>? SearchResults(this WorkflowResult result) =>
        result.Data.Get<List<SearchResultItem>>(GoogleSearchNode.OutputResults);

    /// <inheritdoc cref="SearchResults(WorkflowResult)"/>
    public static List<SearchResultItem>? SearchResults(this WorkflowData data) =>
        data.Get<List<SearchResultItem>>(GoogleSearchNode.OutputResults);

    // ── HTTP Request ──────────────────────────────────────────────────────────

    /// <summary>Returns the HTTP response body (<see cref="HttpRequestNode.OutputResponse"/>).</summary>
    public static object? HttpResponse(this WorkflowResult result) =>
        result.Data.Get<object>(HttpRequestNode.OutputResponse);

    /// <inheritdoc cref="HttpResponse(WorkflowResult)"/>
    public static object? HttpResponse(this WorkflowData data) =>
        data.Get<object>(HttpRequestNode.OutputResponse);

    /// <summary>Returns the HTTP status code (<see cref="HttpRequestNode.OutputStatusCode"/>).</summary>
    public static int HttpStatusCode(this WorkflowResult result) =>
        result.Data.Get<int>(HttpRequestNode.OutputStatusCode);

    // ── File I/O ──────────────────────────────────────────────────────────────

    /// <summary>Returns the file content string (<see cref="FileReaderNode.OutputText"/>).</summary>
    public static string? FileContent(this WorkflowResult result) =>
        result.Data.GetString(FileReaderNode.OutputText);

    /// <inheritdoc cref="FileContent(WorkflowResult)"/>
    public static string? FileContent(this WorkflowData data) =>
        data.GetString(FileReaderNode.OutputText);

    // ── Chunking ──────────────────────────────────────────────────────────────

    /// <summary>Returns the text chunks (<see cref="ChunkTextNode.OutputChunks"/>).</summary>
    public static List<TextChunk>? Chunks(this WorkflowResult result) =>
        result.Data.Get<List<TextChunk>>(ChunkTextNode.OutputChunks);

    /// <inheritdoc cref="Chunks(WorkflowResult)"/>
    public static List<TextChunk>? Chunks(this WorkflowData data) =>
        data.Get<List<TextChunk>>(ChunkTextNode.OutputChunks);

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>Returns the validation flag (<see cref="FilterNode.OutputIsValid"/>).</summary>
    public static bool IsValid(this WorkflowResult result) =>
        result.Data.Get<bool>(FilterNode.OutputIsValid);

    /// <inheritdoc cref="IsValid(WorkflowResult)"/>
    public static bool IsValid(this WorkflowData data) =>
        data.Get<bool>(FilterNode.OutputIsValid);

    /// <summary>Returns validation error messages (<see cref="FilterNode.OutputValidationErrors"/>).</summary>
    public static List<string>? ValidationErrors(this WorkflowResult result) =>
        result.Data.Get<List<string>>(FilterNode.OutputValidationErrors);
}
