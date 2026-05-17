using Twf.Flow.Core;
using Twf.Flow.Core.Http;
using Twf.Flow.Core.Secrets;
using Twf.Flow.Core.Sanitization;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes;
using System.Text;
using System.Text.Json;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Nodes.AI;

/// <summary>
/// Calls any OpenAI-compatible LLM API (OpenAI, Anthropic, Ollama, Azure OpenAI, etc.)
/// 
/// Reads from WorkflowData:
///   - "prompt" or "messages" : the prompt to send
///   - "system_prompt"        : optional system instruction
///
/// Writes to WorkflowData:
///   - "llm_response"    : the model's text response
///   - "llm_model"       : model used
///   - "prompt_tokens"   : tokens used in prompt
///   - "completion_tokens": tokens used in completion
/// </summary>
public sealed class LlmNode : BaseNode
{
    /// <summary>
    /// Gets the name of the node, which is set during initialization. This name is used to identify the node within a workflow and can be customized by the user when creating an instance of the LlmNode.
    /// </summary>
    public override string Name { get; }
    /// <summary>
    /// Gets the category of the node, which is "AI" for this node. This categorization helps users find and organize nodes based on their functionality within the workflow editor.
    /// </summary>
    public override string Category => "AI";
    /// <summary>
    /// Gets Description for the LlmNode
    /// </summary>
    public override string Description => $"Calls {_config.Provider} ({_config.Model}) with the current prompt";

    /// <inheritdoc/>
    public override string IdPrefix => "llm";

    // WorkflowData keys
    public const string InputPrompt            = "prompt";
    public const string InputSystemPrompt      = "system_prompt";
    public const string InputMessages          = "messages";
    public const string OutputResponse         = "llm_response";
    public const string OutputModel            = "llm_model";
    public const string OutputPromptTokens     = "prompt_tokens";
    public const string OutputCompletionTokens = "completion_tokens";

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(InputPrompt,       typeof(string), Required: true,  "Prompt text to send to the model"),
        new(InputSystemPrompt, typeof(string), Required: false, "System instruction (overrides node config)")
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(OutputResponse,         typeof(string), Description: "Model's text response"),
        new(OutputModel,            typeof(string), Description: "Model name used"),
        new(OutputPromptTokens,     typeof(int),    Description: "Tokens consumed by the prompt"),
        new(OutputCompletionTokens, typeof(int),    Description: "Tokens in the completion")
    ];

    /// <summary>
    /// UI schema: parameter form fields shown in the properties panel when configuring the node. This includes options for selecting the LLM provider, model, API key, and other parameters that control the behavior of the node when it is executed within a workflow.
    /// </summary> 
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType = "LlmNode",
        Description = "Send a prompt to any OpenAI-compatible language model",
        Parameters =
        [
            new() { Name = "provider", Label = "Provider", Type = ParameterType.Select, Required = true, DefaultValue = "openai",
                Options =
                [
                    new() { Value = "openai",    Label = "OpenAI" },
                    new() { Value = "anthropic", Label = "Anthropic" },
                    new() { Value = "ollama",    Label = "Ollama (Local)" },
                    new() { Value = "azure",     Label = "Azure OpenAI" },
                ] },
            new() { Name = "model",          Label = "Model",         Type = ParameterType.Text,     Required = true,  DefaultValue = "gpt-4o", Placeholder = "e.g., gpt-4o, claude-3-opus" },
            new() { Name = "apiKey",         Label = "API Key",       Type = ParameterType.Text,     Required = false, Placeholder = "Leave empty to use environment variable" },
            new() { Name = "apiUrl",         Label = "API URL",       Type = ParameterType.Text,     Required = false, Placeholder = "Custom API endpoint (optional)" },
            new() { Name = "systemPrompt",   Label = "System Prompt", Type = ParameterType.TextArea, Required = false, Placeholder = "You are a helpful assistant..." },
            new() { Name = "temperature",    Label = "Temperature",   Type = ParameterType.Number,   Required = false, DefaultValue = 0.7,   MinValue = 0, MaxValue = 2 },
            new() { Name = "maxTokens",      Label = "Max Tokens",    Type = ParameterType.Number,   Required = false, DefaultValue = 1000,  MinValue = 1, MaxValue = 128000 },
            new() { Name = "maintainHistory",Label = "Maintain Chat History", Type = ParameterType.Boolean, Required = false, DefaultValue = false },
        ]
    };

    private readonly LlmConfig _config;
    private readonly IHttpClientProvider _httpProvider;
    private readonly ISecretProvider _secretProvider;
    private readonly IPromptSanitizer _promptSanitizer;

    /// <summary>
    /// Initializes a new instance of the LlmNode with the specified configuration and HTTP provider.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="config">The configuration for the LLM.</param>
    /// <param name="httpProvider">The HTTP client provider for making API calls. If null, uses a default provider.</param>
    /// <param name="secretProvider">The secret provider for resolving API keys. If null, uses a default provider.</param>
    /// <param name="promptSanitizer">The prompt sanitizer for input validation. If null, uses a default sanitizer.</param>
    public LlmNode(
      string name,
        LlmConfig config,
        IHttpClientProvider? httpProvider = null,
        ISecretProvider? secretProvider = null,
  IPromptSanitizer? promptSanitizer = null)
    {
        Name = name;
        _config = config;
        _httpProvider = httpProvider ?? new DefaultHttpClientProvider();
        _secretProvider = secretProvider ?? new DefaultSecretProvider();
        _promptSanitizer = promptSanitizer ?? new DefaultPromptSanitizer();
    }

    /// <summary>
    /// Backward compatibility constructor - accepts HttpClient and wraps it in a provider.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="config">The configuration for the LLM.</param>
    /// <param name="httpClient">An optional HttpClient to use for API calls.</param>
    [Obsolete("Use constructor with IHttpClientProvider instead for better testability.")]
    public LlmNode(string name, LlmConfig config, HttpClient httpClient)
        : this(name, config, new DefaultHttpClientProvider(httpClient))
    {
    }

    /// <summary>
    /// Dictionary constructor — used by the workflow runner for dynamic instantiation.
    /// Parameters: name, provider, model, apiKey, apiUrl, systemPrompt, temperature, maxTokens, maintainHistory.
    /// </summary>
    public LlmNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "LLM",
            BuildConfigFromDict(parameters))
    { }

    private static LlmConfig BuildConfigFromDict(Dictionary<string, object?> p)
    {
        var provider = NodeParameters.GetString(p, "provider") ?? "openai";
        var model = NodeParameters.GetString(p, "model") ?? "gpt-4o";
        var apiKey = NodeParameters.GetString(p, "apiKey") ?? "";
        var apiUrl = NodeParameters.GetString(p, "apiUrl");
        var sysPrompt = NodeParameters.GetString(p, "systemPrompt");
        var temp = (float)NodeParameters.GetDouble(p, "temperature", 0.7);
        var maxTok = NodeParameters.GetInt(p, "maxTokens", 2048);
        var history = NodeParameters.GetBool(p, "maintainHistory");

        return provider.ToLowerInvariant() switch
        {
            "anthropic" => LlmConfig.Anthropic(apiKey, model) with
            {
                DefaultSystemPrompt = sysPrompt,
                Temperature = Temperature.FromValue(temp),
                MaxTokens = TokenCount.FromValue(maxTok),
                MaintainHistory = history
            },
            "ollama" => LlmConfig.Ollama(model, apiUrl ?? "http://localhost:11434") with
            {
                DefaultSystemPrompt = sysPrompt,
                Temperature = Temperature.FromValue(temp),
                MaxTokens = TokenCount.FromValue(maxTok)
            },
            _ => LlmConfig.OpenAI(apiKey, model) with
            {
                ApiEndpoint = apiUrl ?? "https://api.openai.com/v1/chat/completions",
                DefaultSystemPrompt = sysPrompt,
                Temperature = Temperature.FromValue(temp),
                MaxTokens = TokenCount.FromValue(maxTok),
                MaintainHistory = history
            }
        };
    }

    /// <summary>
    /// Executes the node's logic by building the message list, calling the LLM API (either streaming or non-streaming), and returning the response along with token usage information.
    /// </summary>
    /// <param name="input">The input workflow data.</param>
    /// <param name="context">The workflow context.</param>
    /// <param name="nodeCtx">The node execution context.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated workflow data.</returns>
    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        // Sanitize prompt if enabled in config
        var sanitizedInput = input;
        if (_config.SanitizePrompts && _config.SanitizationOptions != null)
        {
            var rawPrompt = input.GetString(InputPrompt) ?? "";
            if (!string.IsNullOrEmpty(rawPrompt))
            {
                var sanitized = _promptSanitizer.Sanitize(rawPrompt, _config.SanitizationOptions);
                sanitizedInput = input.Clone().Set(InputPrompt, sanitized);

                if (rawPrompt != sanitized)
                {
                    nodeCtx.Log($"Prompt sanitized (original: {rawPrompt.Length} chars, sanitized: {sanitized.Length} chars)");
                }
            }
        }

        // Build the messages list
        var messages = BuildMessages(sanitizedInput, context);

        nodeCtx.Log($"Sending {messages.Count} messages to {_config.Model}");
        nodeCtx.Log($"Last message: {messages.Last().Content[..Math.Min(100, messages.Last().Content.Length)]}...");

        var requestBody = BuildRequestBody(messages);

        string llmResponse;
        (int PromptTokens, int CompletionTokens) usage;

        if (_config.Stream)
        {
            (llmResponse, usage) = await StreamApiAsync(requestBody, nodeCtx, context.CancellationToken);
        }
        else
        {
            var response = await CallApiAsync(requestBody, context.CancellationToken);
            llmResponse = ExtractResponse(response);
            usage = ExtractUsage(response);
        }

        nodeCtx.SetMetadata("model", _config.Model);
        nodeCtx.SetMetadata("prompt_tokens", usage.PromptTokens);
        nodeCtx.SetMetadata("completion_tokens", usage.CompletionTokens);
        nodeCtx.Log($"Response length: {llmResponse.Length} chars | Tokens: {usage.PromptTokens}+{usage.CompletionTokens}");

        // Optionally append to conversation history
        if (_config.MaintainHistory)
        {
            context.AppendMessage(ChatMessage.Assistant(llmResponse));
        }

        return input.Clone()
            .Set(OutputResponse,         llmResponse)
            .Set(OutputModel,            _config.Model)
            .Set(OutputPromptTokens,     usage.PromptTokens)
            .Set(OutputCompletionTokens, usage.CompletionTokens);
    }

    private List<(string Role, string Content)> BuildMessages(
        WorkflowData input, WorkflowContext context)
    {
        var messages = new List<(string Role, string Content)>();

        // Add system prompt
        var systemPrompt = input.GetString(InputSystemPrompt) ?? _config.DefaultSystemPrompt;
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(("system", systemPrompt));

        // Optionally include conversation history
        if (_config.MaintainHistory)
        {
            foreach (var msg in context.GetChatHistory())
                messages.Add((msg.Role, msg.Content));
        }

        // Add current prompt or raw messages
        if (input.TryGet<List<(string, string)>>(InputMessages, out var rawMessages) && rawMessages is not null)
        {
            messages.AddRange(rawMessages);
        }
        else
        {
            var prompt = input.GetRequiredString(InputPrompt);

            if (_config.MaintainHistory)
                context.AppendMessage(ChatMessage.User(prompt));

            messages.Add(("user", prompt));
        }

        return messages;
    }

    private Dictionary<string, object?> BuildRequestBody(List<(string Role, string Content)> messages)
    {
        return _config.Provider switch
        {
            LlmProvider.OpenAI or LlmProvider.AzureOpenAI or LlmProvider.Ollama or LlmProvider.Custom =>
                BuildOpenAiStyleBody(messages),
            LlmProvider.Anthropic =>
                BuildAnthropicBody(messages),
            _ => throw new NotSupportedException($"Provider {_config.Provider} not supported")
        };
    }

    private Dictionary<string, object?> BuildOpenAiStyleBody(List<(string Role, string Content)> messages)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = _config.Model,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }),
            ["temperature"] = (float)_config.Temperature,  // Explicit cast from value object
            ["max_tokens"] = (int)_config.MaxTokens,       // Explicit cast from value object
            ["stream"] = _config.Stream
        };
        // include_usage in the final chunk — supported by OpenAI and AzureOpenAI only
        if (_config.Stream && _config.Provider is LlmProvider.OpenAI or LlmProvider.AzureOpenAI)
            body["stream_options"] = new { include_usage = true };
        return body;
    }

    private Dictionary<string, object?> BuildAnthropicBody(List<(string Role, string Content)> messages)
    {
        return new Dictionary<string, object?>
      {
    ["model"] = _config.Model,
            ["max_tokens"] = (int)_config.MaxTokens,
    ["temperature"] = (float)_config.Temperature,
            ["system"] = messages.FirstOrDefault(m => m.Role == "system").Content ?? "",
            ["messages"] = messages
           .Where(m => m.Role != "system")
         .Select(m => new { role = m.Role, content = m.Content }),
         ["stream"] = _config.Stream
  };
    }

    private async Task<JsonDocument> CallApiAsync(object requestBody, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, _config.ApiEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        await AddAuthHeadersAsync(request);

        var httpClient = _httpProvider.GetClient(_config.ApiEndpoint);
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(content);
    }

    private async Task AddAuthHeadersAsync(HttpRequestMessage request)
    {
        // Resolve API key using secret provider
        var apiKey = await _config.GetApiKeyAsync(_secretProvider);

        if (string.IsNullOrEmpty(apiKey)) return;

        if (_config.Provider == LlmProvider.Anthropic)
        {
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
        }
        else
        {
            request.Headers.Authorization =
                   new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private async Task<(string Text, (int PromptTokens, int CompletionTokens) Usage)> StreamApiAsync(
        object requestBody, NodeExecutionContext nodeCtx, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, _config.ApiEndpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        await AddAuthHeadersAsync(request);

        var httpClient = _httpProvider.GetClient(_config.ApiEndpoint);
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var sb = new StringBuilder();
        int promptTokens = 0, completionTokens = 0;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                if (_config.Provider == LlmProvider.Anthropic)
                    ParseAnthropicChunk(doc, sb, ref promptTokens, ref completionTokens);
                else
                    ParseOpenAiChunk(doc, sb, ref promptTokens, ref completionTokens);
            }
            catch (JsonException) { /* skip malformed SSE chunks */ }
        }

        nodeCtx.Log($"Stream complete: {sb.Length} chars");
        return (sb.ToString(), (promptTokens, completionTokens));
    }

    private void ParseOpenAiChunk(
        JsonDocument doc, StringBuilder sb, ref int promptTokens, ref int completionTokens)
    {
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var delta = choices[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var contentEl))
            {
                var chunk = contentEl.GetString() ?? "";
                if (chunk.Length > 0)
                {
                    sb.Append(chunk);
                    _config.OnChunk?.Invoke(chunk);
                }
            }
        }

        // Final chunk carries usage when stream_options.include_usage = true
        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            if (usage.TryGetProperty("prompt_tokens", out var ptEl)) promptTokens = ptEl.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ctEl)) completionTokens = ctEl.GetInt32();
        }
    }

    private void ParseAnthropicChunk(
        JsonDocument doc, StringBuilder sb, ref int promptTokens, ref int completionTokens)
    {
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeEl)) return;

        switch (typeEl.GetString())
        {
            case "message_start":
                if (root.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("usage", out var startUsage) &&
                    startUsage.TryGetProperty("input_tokens", out var it))
                    promptTokens = it.GetInt32();
                break;

            case "content_block_delta":
                if (root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("type", out var dt) && dt.GetString() == "text_delta" &&
                    delta.TryGetProperty("text", out var textEl))
                {
                    var chunk = textEl.GetString() ?? "";
                    if (chunk.Length > 0)
                    {
                        sb.Append(chunk);
                        _config.OnChunk?.Invoke(chunk);
                    }
                }
                break;

            case "message_delta":
                if (root.TryGetProperty("usage", out var endUsage) &&
                    endUsage.TryGetProperty("output_tokens", out var ot))
                    completionTokens = ot.GetInt32();
                break;
        }
    }

    private string ExtractResponse(JsonDocument doc)
    {
        return _config.Provider switch
        {
            LlmProvider.Anthropic =>
                doc.RootElement
                   .GetProperty("content")[0]
                   .GetProperty("text").GetString() ?? "",
            _ =>
                doc.RootElement
                   .GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content").GetString() ?? ""
        };
    }

    private (int PromptTokens, int CompletionTokens) ExtractUsage(JsonDocument doc)
    {
        try
        {
            var usage = doc.RootElement.GetProperty("usage");
            return _config.Provider switch
            {
                LlmProvider.Anthropic => (
                    usage.GetProperty("input_tokens").GetInt32(),
                    usage.GetProperty("output_tokens").GetInt32()),
                _ => (
                    usage.GetProperty("prompt_tokens").GetInt32(),
                    usage.GetProperty("completion_tokens").GetInt32())
            };
        }
        catch { return (0, 0); }
    }
}

// ─── Configuration ────────────────────────────────────────────────────────────
/// <summary>
/// Configuration for the LlmNode, including provider selection, model name, API endpoint, and other parameters.
/// </summary>
public enum LlmProvider
{
    /// <summary>
    /// Open AI Provider
    /// </summary>
    OpenAI,
    /// <summary>
    /// Anthopic Provider
    /// </summary>
    Anthropic,
    /// <summary>
    /// Azure OpenAI Provider
    /// </summary>
    AzureOpenAI,
    /// <summary>
    /// Ollama Provider
    /// </summary>
    Ollama,
    /// <summary>
    /// Custom Provider - allows users to specify their own API endpoint and request format. The node will use the OpenAI-style request body by default, but users can customize the BuildRequestBody method for different formats if needed.
    /// </summary>
    Custom
}
