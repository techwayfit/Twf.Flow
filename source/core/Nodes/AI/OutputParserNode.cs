using Twf.Flow.Core;
using Twf.Flow.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Twf.Flow.Nodes.AI;

/// <summary>
/// Parses structured JSON output from LLM responses.
/// Handles markdown code fences, extracts JSON objects/arrays,
/// and maps extracted fields into WorkflowData keys.
///
/// Reads from WorkflowData:
///   - "llm_response" : raw text from the LLM
///
/// Writes to WorkflowData:
///   - The extracted JSON fields as individual keys
///   - "parsed_output" : the full parsed object
/// </summary>
/// <example>
/// <code>
/// // Example 1: Auto-parse LLM JSON response
/// var parser = new OutputParserNode("ParseResponse");
/// 
/// var llmOutput = new WorkflowData().Set("llm_response", """
///   {
/// "sentiment": "positive",
///   "confidence": 0.95,
///  "entities": ["product", "pricing"]
///     }
///     """);
/// 
/// var result = await parser.ExecuteAsync(llmOutput, context);
/// var sentiment = result.Data.Get&lt;string&gt;("sentiment");      // "positive"
/// var confidence = result.Data.Get&lt;double&gt;("confidence");    // 0.95
/// var entities = result.Data.Get&lt;List&lt;object&gt;&gt;("entities"); // ["product", "pricing"]
/// var fullJson = result.Data.Get&lt;Dictionary&lt;string, object?&gt;&gt;("parsed_output");
/// 
/// // Example 2: Extract from markdown code fence
/// var fencedParser = new OutputParserNode("ParseFenced");
/// 
/// var llmWithMarkdown = new WorkflowData().Set("llm_response", """
///     Here's the analysis you requested:
///     
///     ```json
///     {
///     "summary": "Great product",
///         "score": 8.5,
///      "recommend": true
///     }
///   ```
///  
///     This shows the user is satisfied.
///     """);
/// 
/// var result = await fencedParser.ExecuteAsync(llmWithMarkdown, context);
/// var summary = result.Data.Get&lt;string&gt;("summary");     // "Great product"
/// var score = result.Data.Get&lt;double&gt;("score");         // 8.5
/// var recommend = result.Data.Get&lt;bool&gt;("recommend"); // true
/// 
/// // Example 3: Field mapping (rename JSON keys)
/// var mappingParser = new OutputParserNode(
///   "MapFields",
///   fieldMapping: new Dictionary&lt;string, string&gt;
///     {
///         ["sentiment"] = "user_sentiment",    // Rename sentiment → user_sentiment
///       ["score"] = "confidence_level"    // Rename score → confidence_level
///     }
/// );
/// 
/// var llmJson = new WorkflowData().Set("llm_response", """
/// {"sentiment": "negative", "score": 0.82}
/// """);
/// 
/// var result = await mappingParser.ExecuteAsync(llmJson, context);
/// var sentiment = result.Data.Get&lt;string&gt;("user_sentiment");     // "negative"
/// var confidence = result.Data.Get&lt;double&gt;("confidence_level");  // 0.82
/// // Note: original keys are NOT added to WorkflowData when using fieldMapping
/// 
/// // Example 4: Strict mode (fail on invalid JSON)
/// var strictParser = OutputParserNode.Strict("StrictParse");
/// 
/// var invalidJson = new WorkflowData().Set("llm_response", "This is not JSON");
/// 
/// try
/// {
///     var result = await strictParser.ExecuteAsync(invalidJson, context);
/// }
/// catch (InvalidOperationException ex)
/// {
/// // Throws exception: "OutputParser: Could not extract JSON from LLM response..."
/// }
/// 
/// // Example 5: Lenient mode (pass-through on failure)
/// var lenientParser = new OutputParserNode("Lenient", strict: false);
/// 
/// var noJson = new WorkflowData()
///     .Set("llm_response", "I couldn't generate JSON for that request.");
/// 
/// var result = await lenientParser.ExecuteAsync(noJson, context);
/// // Result: original data unchanged, no exception thrown
/// // Logs: "⚠️  No JSON found in response — passing data through unchanged"
/// 
/// // Example 6: Convenience factory with field mapping
/// var factoryParser = OutputParserNode.WithMapping(
///  "MapSpecific",
///     ("title", "article_title"),
///     ("author", "article_author"),
///     ("published", "publish_date")
/// );
/// 
/// var article = new WorkflowData().Set("llm_response", """
/// {
///     "title": "AI Trends 2025",
///   "author": "Jane Doe",
///         "published": "2025-01-15"
///  }
/// """);
/// 
/// var result = await factoryParser.ExecuteAsync(article, context);
/// var title = result.Data.Get&lt;string&gt;("article_title");
/// var author = result.Data.Get&lt;string&gt;("article_author");
/// var date = result.Data.Get&lt;string&gt;("publish_date");
/// 
/// // Example 7: Use in LLM workflow
/// var workflow = WorkflowBuilder.Create("StructuredExtraction")
///     .AddNode(new PromptBuilderNode("BuildPrompt", 
///      "Extract: {{text}}\nReturn JSON: {\"category\": \"...\", \"priority\": 1-5}"))
///     .AddNode(new LlmNode("ExtractData", llmConfig))
///     .AddNode(new OutputParserNode("ParseJSON"))
///  .AddNode(new FilterNode("ValidateParsed")
///   .Require("category")
///         .Custom("priority", 
///   data => data.Get&lt;int&gt;("priority") >= 1 && data.Get&lt;int&gt;("priority") <= 5,
///             "Priority must be 1-5"));
/// 
/// var input = new WorkflowData().Set("text", "URGENT: Server down!");
/// var result = await workflow.RunAsync(input);
/// var category = result.Data.Get&lt;string&gt;("category");
/// var priority = result.Data.Get&lt;int&gt;("priority");
/// 
/// // Example 8: Handle LLM that returns array
/// var arrayParser = new OutputParserNode("ParseArray");
/// 
/// var llmArray = new WorkflowData().Set("llm_response", """
///     [
// {"name": "Item 1", "value": 10},
///      {"name": "Item 2", "value": 20}
///     ]
///     """);
/// 
/// var result = await arrayParser.ExecuteAsync(llmArray, context);
/// var items = result.Data.Get&lt;List&lt;object&gt;&gt;("parsed_output");
/// // Access array items from parsed_output
/// </code>
/// </example>
public sealed class OutputParserNode : BaseNode
{
    private readonly string _name;

    public override string Name => _name;
    public override string Category => "AI";
    public override string Description => "Parses JSON content from LLM output into structured workflow data";

    /// <inheritdoc/>

    // WorkflowData keys
    public const string InputResponse   = "llm_response";
    public const string OutputParsedOutput = "parsed_output";

    private readonly Dictionary<string, string>? _fieldMapping;
    private readonly bool _strict;

    /// <param name="fieldMapping">Optional mapping of JSON keys to WorkflowData keys.
    ///     If null, all JSON keys are written directly to WorkflowData.</param>
    /// <param name="strict">If true, throw if JSON cannot be parsed. If false, pass through.</param>
    public OutputParserNode(
        string name = "OutputParser",
        Dictionary<string, string>? fieldMapping = null,
        bool strict = false)
    {
        _name = name;
        _fieldMapping = fieldMapping;
        _strict = strict;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public OutputParserNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Output Parser",
            NodeParameters.GetStringDict(parameters, "fieldMapping"),
            NodeParameters.GetBool(parameters, "strict"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var rawResponse = input.GetRequiredString(InputResponse);
        var json = ExtractJson(rawResponse);

        if (json is null)
        {
            if (_strict)
                throw new InvalidOperationException(
                    $"OutputParser: Could not extract JSON from LLM response. Raw: {rawResponse[..Math.Min(200, rawResponse.Length)]}");

            nodeCtx.Log("⚠️  No JSON found in response — passing data through unchanged");
            return Task.FromResult(input.Clone());
        }

        var output = input.Clone();

        try
        {
            var doc = JsonDocument.Parse(json);
            var parsedDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                             ?? new Dictionary<string, object?>();

            output.Set(OutputParsedOutput, parsedDict);

            if (_fieldMapping is not null)
            {
                // Map specific JSON keys to WorkflowData keys
                foreach (var (jsonKey, dataKey) in _fieldMapping)
                {
                    if (parsedDict.TryGetValue(jsonKey, out var val))
                    {
                        output.Set(dataKey, val);
                        nodeCtx.Log($"Mapped '{jsonKey}' → '{dataKey}'");
                    }
                    else
                    {
                        nodeCtx.Log($"⚠️  JSON key '{jsonKey}' not found in response");
                    }
                }
            }
            else
            {
                // Write all JSON keys directly to WorkflowData
                foreach (var (k, v) in parsedDict)
                {
                    output.Set(k, v);
                }
                nodeCtx.Log($"Extracted {parsedDict.Count} fields from JSON");
            }

            nodeCtx.SetMetadata("parsed_keys", string.Join(", ", parsedDict.Keys));
        }
        catch (JsonException ex)
        {
            if (_strict)
                throw new InvalidOperationException($"OutputParser: Invalid JSON: {ex.Message}");

            nodeCtx.Log($"⚠️  JSON parse error: {ex.Message}");
        }

        return Task.FromResult(output);
    }

    /// <summary>
    /// Extracts a JSON object or array from text that may contain markdown fences,
    /// explanation text before/after, etc.
    /// </summary>
    private static string? ExtractJson(string text)
    {
        // Try markdown code fence first: ```json ... ``` or ``` ... ```
        var fenceMatch = Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\}|\[[\s\S]*?\])\s*```");
        if (fenceMatch.Success)
            return fenceMatch.Groups[1].Value.Trim();

        // Try to find a raw JSON object
        var objMatch = Regex.Match(text, @"\{[\s\S]*\}");
        if (objMatch.Success)
        {
            try
            {
                JsonDocument.Parse(objMatch.Value);
                return objMatch.Value;
            }
            catch { /* not valid JSON */ }
        }

        // Try to find a raw JSON array
        var arrMatch = Regex.Match(text, @"\[[\s\S]*\]");
        if (arrMatch.Success)
        {
            try
            {
                JsonDocument.Parse(arrMatch.Value);
                return arrMatch.Value;
            }
            catch { /* not valid JSON */ }
        }

        return null;
    }

    // ─── Convenience Factory Methods ──────────────────────────────────────────

    public static OutputParserNode WithMapping(string name,params (string JsonKey, string DataKey)[] mappings) =>
        new(name,mappings.ToDictionary(m => m.JsonKey, m => m.DataKey));

    public static OutputParserNode Strict(string name) => new(name,strict: true);
}
