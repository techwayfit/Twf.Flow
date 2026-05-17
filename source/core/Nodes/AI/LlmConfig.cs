using Twf.Flow.Core.Secrets;
using Twf.Flow.Core.Sanitization;
using Twf.Flow.Core.ValueObjects;

namespace Twf.Flow.Nodes.AI;
/// <summary>
/// Llm Configuration
/// </summary>
public sealed record LlmConfig
{
    /// <summary>
    /// Llm Provider
    /// </summary>
    public LlmProvider Provider { get; init; } = LlmProvider.OpenAI;
    /// <summary>
    /// AI Model
    /// </summary>
    public string Model { get; init; } = "gpt-4o";

    /// <summary>
    /// API Key - can be a plain value or a secret reference (e.g., "env:OPENAI_API_KEY").
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - Plain string: "sk-abc123..." (backward compatible)
    /// - Environment variable: "env:OPENAI_API_KEY"
    /// - File reference: "file:./secrets/api-key.txt"
    /// 
    /// The actual secret resolution happens at runtime via ISecretProvider.
    /// </remarks>
    public string ApiKey { get; init; } = "";

    /// <summary>
    /// Secure API key reference (preferred over plain ApiKey).
    /// </summary>
    /// <remarks>
    /// When set, this takes precedence over <see cref="ApiKey"/>.
    /// Automatically resolves secret references like "env:OPENAI_API_KEY".
    /// </remarks>
    public SecretReference? ApiKeyReference { get; init; }

    /// <summary>
    /// Api End point
    /// </summary>
    public string ApiEndpoint { get; init; } = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// LLM temperature parameter (0.0 - 2.0). Higher values produce more creative responses.
    /// Default is 0.7 (Balanced).
    /// </summary>
    /// <remarks>
    /// Use predefined values or create custom:
    /// <code>
    /// Temperature.Focused      // 0.3 - precise, deterministic
    /// Temperature.Balanced     // 0.7 - default, general use
    /// Temperature.Creative     // 1.0 - exploratory, varied
    /// Temperature.FromValue(0.5f) // custom value
    /// </code>
    /// </remarks>
    public Temperature Temperature { get; init; } = Temperature.Balanced;

    /// <summary>
    /// Max tokens to generate in the response. Default is 2048 (Standard).
    /// </summary>
    /// <remarks>
    /// Use predefined values or create custom:
    /// <code>
    /// TokenCount.Short    // 256 - quick responses
    /// TokenCount.Standard   // 2048 - typical use
    /// TokenCount.Extended   // 4096 - detailed responses
    /// TokenCount.FromValue(1000) // custom value
    /// </code>
    /// </remarks>
    public TokenCount MaxTokens { get; init; } = TokenCount.Standard;

    /// <summary>
    /// Default system prompt to use if none is provided in the input.
    /// </summary>
    public string? DefaultSystemPrompt { get; init; }
    /// <summary>
    /// Whether to maintain conversation history.
    /// </summary>
    public bool MaintainHistory { get; init; } = false;
    /// <summary>
    /// When true, the node streams the response via SSE and delivers chunks via <see cref="OnChunk"/>.
    /// </summary>
    public bool Stream { get; init; } = false;
    /// <summary>Called with each text chunk as it arrives during streaming. Ignored when <see cref="Stream"/> is false.</summary>
    public Action<string>? OnChunk { get; init; }

    /// <summary>
    /// Whether to sanitize prompts before sending to LLM.
    /// </summary>
    /// <remarks>
    /// When enabled, prompts are validated and sanitized according to
    /// <see cref="SanitizationOptions"/> to prevent injection attacks.
    /// Recommended for production use.
    /// </remarks>
    public bool SanitizePrompts { get; init; } = true;

    /// <summary>
    /// Options for prompt sanitization and validation.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="SanitizePrompts"/> is true.
    /// Defaults to <see cref="PromptSanitizationOptions.Default"/> if null.
    /// </remarks>
    public PromptSanitizationOptions? SanitizationOptions { get; init; }

    /// <summary>
    /// Gets the effective API key, resolving secret references if needed.
    /// </summary>
    /// <param name="secretProvider">Optional secret provider for resolving references.</param>
    /// <returns>The resolved API key value.</returns>
    public async Task<string> GetApiKeyAsync(ISecretProvider? secretProvider = null)
    {
        // Prefer ApiKeyReference if set
        if (ApiKeyReference != null)
        {
            var provider = secretProvider ?? new DefaultSecretProvider();
            return await ApiKeyReference.ResolveAsync(provider);
        }

        // Fall back to ApiKey (backward compatibility)
        if (!string.IsNullOrEmpty(ApiKey))
        {
            // Check if ApiKey is actually a reference string
            if (secretProvider != null && secretProvider.IsSecretReference(ApiKey))
            {
                return await secretProvider.GetSecretAsync(ApiKey);
            }
            return ApiKey;
        }

        return string.Empty;
    }

    // ─── Presets ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Factory method for creating an OpenAI configuration with the specified API key and model.
    /// </summary>
    /// <param name="apiKey">API key or secret reference (e.g., "env:OPENAI_API_KEY").</param>
    /// <param name="model">Model name.</param>
    /// <returns>LlmConfig instance.</returns>
    public static LlmConfig OpenAI(string apiKey, string model = "gpt-4o") => new()
    {
        Provider = LlmProvider.OpenAI,
        Model = model,
        ApiKey = apiKey,
        ApiEndpoint = "https://api.openai.com/v1/chat/completions"
    };

    /// <summary>
    /// Factory method for creating an OpenAI configuration with a secure secret reference.
    /// </summary>
    /// <param name="apiKeyReference">Secure API key reference.</param>
    /// <param name="model">Model name.</param>
    /// <param name="sanitizationMode">Prompt sanitization mode.</param>
    /// <returns>LlmConfig instance with sanitization enabled.</returns>
    public static LlmConfig OpenAISecure(
        SecretReference apiKeyReference,
        string model = "gpt-4o",
     PromptSanitizationMode sanitizationMode = PromptSanitizationMode.EscapeSpecialChars) => new()
     {
         Provider = LlmProvider.OpenAI,
         Model = model,
         ApiKeyReference = apiKeyReference,
         ApiEndpoint = "https://api.openai.com/v1/chat/completions",
         SanitizePrompts = true,
         SanitizationOptions = new PromptSanitizationOptions
         {
             Mode = sanitizationMode,
             ValidationLevel = PromptValidationLevel.Moderate
         }
     };

    /// <summary>
    /// Factory method for creating an Anthropic configuration with the specified API key and model.
    /// </summary>
    /// <param name="apiKey">API key or secret reference.</param>
    /// <param name="model">Model name.</param>
    /// <returns>LlmConfig instance.</returns>
    public static LlmConfig Anthropic(string apiKey, string model = "claude-sonnet-4-20250514") => new()
    {
        Provider = LlmProvider.Anthropic,
        Model = model,
        ApiKey = apiKey,
        ApiEndpoint = "https://api.anthropic.com/v1/messages"
    };

    /// <summary>
    /// Factory method for creating an Anthropic configuration with a secure secret reference.
    /// </summary>
    /// <param name="apiKeyReference">Secure API key reference.</param>
    /// <param name="model">Model name.</param>
    /// <returns>LlmConfig instance.</returns>
    public static LlmConfig AnthropicSecure(SecretReference apiKeyReference, string model = "claude-sonnet-4-20250514") => new()
    {
        Provider = LlmProvider.Anthropic,
        Model = model,
        ApiKeyReference = apiKeyReference,
        ApiEndpoint = "https://api.anthropic.com/v1/messages"
    };

    /// <summary>
    /// Factory method for creating an Ollama configuration with the specified model and host. Assumes no API key is required.
    /// </summary>
    /// <param name="model">Model name.</param>
    /// <param name="host">Ollama host URL.</param>
    /// <returns>LlmConfig instance.</returns>
    public static LlmConfig Ollama(string model = "llama3.2", string host = "http://localhost:11434") => new()
    {
        Provider = LlmProvider.Ollama,
        Model = model,
        ApiEndpoint = $"{host}/v1/chat/completions"
    };

    /// <summary>
    /// Factory method for creating a custom LLM server configuration with the specified model, API key, and endpoint.
    /// </summary>
    /// <param name="model">Model name.</param>
    /// <param name="apiKey">API key or secret reference.</param>
    /// <param name="apiEndpoint">API endpoint URL.</param>
    /// <returns>LlmConfig instance.</returns>
    public static LlmConfig LmServer(string model, string apiKey, string apiEndpoint) => new()
    {
        Provider = LlmProvider.Custom,
        Model = model,
        ApiKey = apiKey,
        ApiEndpoint = apiEndpoint
    };
}
