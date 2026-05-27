using Twf.Flow.Core;
using Twf.Flow.Nodes;
using System.Text;
using System.Text.Json;

namespace Twf.Flow.Nodes.IO;

// ═══════════════════════════════════════════════════════════════════════════════
// HttpRequestNode — Make HTTP API calls from within a workflow
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Makes HTTP requests to external APIs — REST APIs, webhooks, data sources.
/// Like n8n's HTTP Request node.
///
/// Reads from WorkflowData:
///   - Keys specified in UrlTemplate as {{variable}} placeholders
///   - "request_body" : if method is POST/PUT and no body configured
///
/// Writes to WorkflowData:
///   - "http_response"      : response body (parsed JSON or raw string)
///   - "http_status_code"   : HTTP status code
///   - "http_headers"       : response headers dictionary
/// </summary>
public sealed class HttpRequestNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "IO";
    public override string Description =>
        $"HTTP {_config.Method} {_config.UrlTemplate}";

    /// <inheritdoc/>

    // WorkflowData keys
    public const string InputRequestBody  = "request_body";
    public const string OutputResponse    = "http_response";
    public const string OutputStatusCode  = "http_status_code";
    public const string OutputHeaders     = "http_headers";

    private readonly HttpRequestConfig _config;
    private readonly HttpClient _httpClient;

    public HttpRequestNode(string name, HttpRequestConfig config, HttpClient? httpClient = null)
    {
        Name = name;
        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = config.Timeout;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public HttpRequestNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "HTTP Request",
            new HttpRequestConfig
            {
                Method      = (NodeParameters.GetString(parameters, "method") ?? "GET").ToUpperInvariant(),
                UrlTemplate = NodeParameters.GetString(parameters, "url")
                              ?? NodeParameters.GetString(parameters, "urlTemplate") ?? "",
                Headers     = NodeParameters.GetStringDict(parameters, "headers")
                              ?? new Dictionary<string, string>(),
                ThrowOnError = NodeParameters.GetBool(parameters, "throwOnError", true),
                Timeout      = TimeSpan.FromMilliseconds(
                              NodeParameters.GetDouble(parameters, "timeoutMs", 30_000))
            })
    { }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        // Build URL with variable substitution
        var url = RenderUrl(_config.UrlTemplate, input);
        nodeCtx.Log($"{_config.Method} {url}");

        using var request = new HttpRequestMessage(
            new HttpMethod(_config.Method), url);

        // Add headers
        foreach (var (k, v) in _config.Headers)
            request.Headers.TryAddWithoutValidation(k, v);

        // Add body for POST/PUT/PATCH
        if (_config.Method is "POST" or "PUT" or "PATCH")
        {
            var body = _config.Body ?? input.Get<object>(InputRequestBody);
            if (body is not null)
            {
                var json = body is string s ? s : JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
        }

        var response = await _httpClient.SendAsync(request, context.CancellationToken);
        var statusCode = (int)response.StatusCode;

        nodeCtx.Log($"Response: {statusCode} {response.StatusCode}");
        nodeCtx.SetMetadata("status_code", statusCode);

        if (!response.IsSuccessStatusCode && _config.ThrowOnError)
            throw new HttpRequestException(
                $"HTTP {_config.Method} {url} failed with status {statusCode}");

        var responseBody = await response.Content.ReadAsStringAsync(context.CancellationToken);

        // Parse JSON if response is JSON
        object parsedResponse = responseBody;
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            try
            {
                parsedResponse = JsonSerializer.Deserialize<object>(responseBody)!;
            }
            catch { /* keep as string */ }
        }

        var headers = response.Headers.ToDictionary(
            h => h.Key, h => string.Join(", ", h.Value));

        return input.Clone()
            .Set(OutputResponse,   parsedResponse)
            .Set(OutputStatusCode, statusCode)
            .Set(OutputHeaders,    headers);
    }

    private string RenderUrl(string template, WorkflowData data)
    {
        return System.Text.RegularExpressions.Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
        {
            var key = m.Groups[1].Value;
            return data.GetString(key) ?? m.Value;
        });
    }

    // ─── Prebuilt HTTP nodes ──────────────────────────────────────────────────

    public static HttpRequestNode Get(string name, string url,
        Dictionary<string, string>? headers = null) =>
        new(name, new HttpRequestConfig
        {
            Method = "GET",
            UrlTemplate = url,
            Headers = headers ?? new()
        });

    public static HttpRequestNode Post(string name, string url,
        object? body = null,
        Dictionary<string, string>? headers = null) =>
        new(name, new HttpRequestConfig
        {
            Method = "POST",
            UrlTemplate = url,
            Body = body,
            Headers = headers ?? new()
        });
}

public sealed class HttpRequestConfig
{
    public string Method { get; init; } = "GET";
    public string UrlTemplate { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new();
    public object? Body { get; init; }
    public bool ThrowOnError { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
