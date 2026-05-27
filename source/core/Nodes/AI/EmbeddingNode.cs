using Twf.Flow.Core;
using Twf.Flow.Nodes;
using System.Text;
using System.Text.Json;

namespace Twf.Flow.Nodes.AI;

/// <summary>
/// Generates vector embeddings for text using OpenAI Embeddings API or compatible endpoint.
/// Core building block for RAG pipelines, semantic search, and similarity matching.
///
/// Reads from WorkflowData:
///   - "text"  or "texts" (List&lt;string&gt;) : text(s) to embed
///
/// Writes to WorkflowData:
///   - "embedding"  : float[] — single embedding
///   - "embeddings" : List&lt;float[]&gt; — batch embeddings
///   - "embedding_model" : model name used
/// </summary>
public sealed class EmbeddingNode : BaseNode
{
    
    public override string Name { get; }
    public override string Category => "AI";
    public override string Description =>
        $"Generates vector embeddings using {_config.Model}";

    /// <inheritdoc/>

    // WorkflowData keys
    public const string InputText       = "text";
    public const string InputTexts      = "texts";
    public const string OutputEmbedding  = "embedding";
    public const string OutputEmbeddings = "embeddings";
    public const string OutputModel      = "embedding_model";

    /// <inheritdoc/>

    /// <inheritdoc/>

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>

    private readonly EmbeddingConfig _config;
    private readonly HttpClient _httpClient;

    public EmbeddingNode(string name, EmbeddingConfig config, HttpClient? httpClient = null)
    {
        Name = name;
        _config = config;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public EmbeddingNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Embedding",
            new EmbeddingConfig
            {
                Model       = NodeParameters.GetString(parameters, "model") ?? "text-embedding-3-small",
                ApiKey      = NodeParameters.GetString(parameters, "apiKey") ?? "",
                ApiEndpoint = NodeParameters.GetString(parameters, "apiUrl")
                              ?? "https://api.openai.com/v1/embeddings"
            })
    { }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var output = input.Clone();

        // Single text
        if (input.TryGet<string>(InputText, out var singleText) && singleText is not null)
        {
            nodeCtx.Log($"Embedding single text ({singleText.Length} chars)");
            var embedding = await GetEmbeddingAsync(singleText, context.CancellationToken);
            output.Set(OutputEmbedding, embedding);
            nodeCtx.SetMetadata("dimensions", embedding.Length);
        }
        // Batch texts
        else if (input.TryGet<List<string>>(InputTexts, out var texts) && texts is not null)
        {
            nodeCtx.Log($"Embedding {texts.Count} texts in batch");
            var embeddings = new List<float[]>();
            foreach (var text in texts)
            {
                var emb = await GetEmbeddingAsync(text, context.CancellationToken);
                embeddings.Add(emb);
            }
            output.Set(OutputEmbeddings, embeddings);
            nodeCtx.SetMetadata("batch_size", texts.Count);
        }
        else
        {
            throw new InvalidOperationException(
                $"EmbeddingNode requires '{InputText}' (string) or '{InputTexts}' (List<string>) in WorkflowData");
        }

        output.Set(OutputModel, _config.Model);
        nodeCtx.SetMetadata("model", _config.Model);

        return output;
    }

    private async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = _config.Model,
            input = text,
            encoding_format = "float"
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _config.ApiEndpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_config.ApiKey))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var floats = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return floats;
    }

    /// <summary>Compute cosine similarity between two embeddings (range: -1 to 1).</summary>
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embeddings must have the same dimensions");

        float dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}

public sealed class EmbeddingConfig
{
    public string Model { get; init; } = "text-embedding-3-small";
    public string ApiKey { get; init; } = "";
    public string ApiEndpoint { get; init; } = "https://api.openai.com/v1/embeddings";

    public static EmbeddingConfig OpenAI(string apiKey, string model = "text-embedding-3-small") => new()
    {
        Model = model, ApiKey = apiKey
    };
}
