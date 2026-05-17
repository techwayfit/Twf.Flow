namespace Twf.Flow.Core.ValueObjects;

/// <summary>
/// Value object representing a token count limit for LLM requests.
/// Valid range: 1 to 128,000 (based on current model limits).
/// </summary>
/// <remarks>
/// Immutable value object with built-in validation.
/// Use factory methods to create instances:
/// <code>
/// var tokens = TokenCount.FromValue(1000);
/// var standard = TokenCount.Standard;  // 2048
/// var extended = TokenCount.Extended;  // 8000
/// </code>
/// </remarks>
public readonly record struct TokenCount
{
    /// <summary>
    /// The token count value (1 to 128,000).
    /// </summary>
    public int Value { get; }

    private TokenCount(int value)
    {
        if (value < 1 || value > 128_000)
            throw new ArgumentOutOfRangeException(
  nameof(value),
                value,
          "Token count must be between 1 and 128,000");

        Value = value;
    }

    /// <summary>
    /// Creates a TokenCount from an integer value.
    /// </summary>
    /// <param name="value">Token count (1 to 128,000).</param>
    /// <returns>A validated TokenCount instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside valid range.</exception>
    public static TokenCount FromValue(int value) => new(value);

    /// <summary>
    /// Predefined: Minimal response (256 tokens).
    /// Best for: short answers, classifications, quick responses.
    /// </summary>
    public static TokenCount Short => new(256);

    /// <summary>
    /// Predefined: Standard response (2048 tokens).
    /// Best for: typical chatbot responses, standard completions.
    /// </summary>
    public static TokenCount Standard => new(2048);

    /// <summary>
    /// Predefined: Extended response (4096 tokens).
    /// Best for: detailed explanations, longer content.
    /// </summary>
    public static TokenCount Extended => new(4096);

    /// <summary>
    /// Predefined: Long-form content (8000 tokens).
    /// Best for: articles, comprehensive analysis, documentation.
    /// </summary>
    public static TokenCount LongForm => new(8000);

    /// <summary>
    /// Predefined: Maximum standard context (16000 tokens).
    /// Best for: processing large documents, extensive generation.
    /// </summary>
    public static TokenCount Large => new(16_000);

    /// <summary>
    /// Predefined: Very large context (32000 tokens).
    /// Best for: book chapters, comprehensive reports.
    /// </summary>
    public static TokenCount VeryLarge => new(32_000);

    /// <summary>
    /// Implicit conversion to int for backward compatibility.
    /// </summary>
    public static implicit operator int(TokenCount count) => count.Value;

    /// <summary>
    /// Explicit conversion from int with validation.
    /// </summary>
    public static explicit operator TokenCount(int value) => FromValue(value);

    /// <inheritdoc/>
    public override string ToString() => $"{Value:N0} tokens";
}
