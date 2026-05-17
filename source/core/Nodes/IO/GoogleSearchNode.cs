using Twf.Flow.Core;
using System.Text.Json;
using System.Web;

namespace Twf.Flow.Nodes.IO;

// ═══════════════════════════════════════════════════════════════════════════════
// GoogleSearchNode — Run a real Google search via SerpApi and return structured results
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Executes a real Google search via SerpApi (https://serpapi.com) and returns
/// structured organic results. No Programmable Search Engine required — searches
/// the entire web exactly as Google does.
///
/// Get a free API key at https://serpapi.com (100 searches/month on free tier).
///
/// Reads from WorkflowData:
///   - "search_query"         : the search query string (required)
///   - "search_results_count" : number of results to fetch (1–100, default 5)
///
/// Writes to WorkflowData:
///   - "search_results"       : List&lt;SearchResultItem&gt; — ranked organic hits
///   - "search_query_used"    : the query that was actually submitted
///   - "search_results_count" : how many results were returned
/// </summary>
public sealed class GoogleSearchNode : BaseNode
{
    public override string Name => Schema.NodeType;
    public override string Category => "IO";
    public override string Description => Schema.Description;

    public override string IdPrefix => "gsearch";

    // WorkflowData keys
    public const string InputQuery        = "search_query";
    public const string InputResultCount  = "search_results_count";
    public const string OutputResults     = "search_results";
    public const string OutputQueryUsed   = "search_query_used";
    public const string OutputResultCount = "search_results_count";

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(InputQuery,       typeof(string), Required: true,  Description: "Search query string"),
        new(InputResultCount, typeof(int),    Required: false, Description: "Number of results to return (default 5)")
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(OutputResults,     typeof(List<SearchResultItem>), Description: "Parsed organic search result items"),
        new(OutputQueryUsed,   typeof(string),                 Description: "Query submitted to Google"),
        new(OutputResultCount, typeof(int),                    Description: "Number of results returned")
    ];

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "GoogleSearchNode",
        Description = "Search Google via SerpApi and return structured organic results",
        Parameters  =
        [
            new() { Name = "apiKey", Label = "SerpApi Key", Type = ParameterType.Text, Required = true,
                Placeholder = "your-serpapi-key",
                Description = "Get a free key at serpapi.com (100 searches/month)" },
        ]
    };

    /// <summary>Dictionary constructor for dynamic instantiation by the runner.</summary>
    public GoogleSearchNode(Dictionary<string, object?> parameters)
        : this(NodeParameters.GetString(parameters, "apiKey") ?? "")
    { }

    private const string SerpApiBase = "https://serpapi.com/search.json";

    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    /// <param name="apiKey">SerpApi API key. Get one free at https://serpapi.com.</param>
    /// <param name="httpClient">Optional custom HttpClient (e.g. for testing).</param>
    public GoogleSearchNode(string apiKey, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                "GoogleSearchNode: 'apiKey' is required. Configure it in the node properties panel.");

        var query = input.GetRequiredString(InputQuery);
        var count = input.Get<int>(InputResultCount);
        if (count <= 0) count = 5;

        nodeCtx.Log($"Searching Google: \"{query}\" (top {count})");

        var url = BuildUrl(query, count);
        var response = await _httpClient.GetAsync(url, context.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(context.CancellationToken);
            throw new HttpRequestException(
                $"SerpApi returned {(int)response.StatusCode}: {error}");
        }

        var json = await response.Content.ReadAsStringAsync(context.CancellationToken);
        var results = ParseResults(json, count);

        nodeCtx.Log($"Returned {results.Count} results");
        nodeCtx.SetMetadata("result_count", results.Count);
        nodeCtx.SetMetadata("query", query);

        return input.Clone()
            .Set(OutputResults,     results)
            .Set(OutputQueryUsed,   query)
            .Set(OutputResultCount, results.Count);
    }

    private string BuildUrl(string query, int count)
    {
        var q = HttpUtility.UrlEncode(query);
        return $"{SerpApiBase}?engine=google&q={q}&num={count}&api_key={_apiKey}";
    }

    private static List<SearchResultItem> ParseResults(string json, int limit)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<SearchResultItem>();

        // SerpApi returns organic results under "organic_results"
        if (!doc.RootElement.TryGetProperty("organic_results", out var items))
            return results;

        foreach (var item in items.EnumerateArray())
        {
            if (results.Count >= limit) break;

            results.Add(new SearchResultItem(
                Title:       item.TryGetProperty("title",   out var t) ? t.GetString() ?? "" : "",
                Description: item.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "",
                LinkedPage:  item.TryGetProperty("link",    out var l) ? l.GetString() ?? "" : ""
            ));
        }

        return results;
    }
}

/// <summary>
/// A single organic result from a Google search.
/// </summary>
/// <param name="Title">The page title as shown in search results.</param>
/// <param name="Description">The snippet / summary text shown under the title.</param>
/// <param name="LinkedPage">The full URL of the result page.</param>
public sealed record SearchResultItem(
    string Title,
    string Description,
    string LinkedPage
);
