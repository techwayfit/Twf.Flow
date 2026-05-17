namespace Twf.Flow.Core.Sanitization;

/// <summary>
/// Defines how prompt input should be sanitized before sending to LLM.
/// </summary>
public enum PromptSanitizationMode
{
    /// <summary>
    /// No sanitization - use raw input as-is (not recommended for production).
    /// </summary>
    None = 0,

    /// <summary>
/// Basic sanitization - remove control characters and normalize whitespace.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Escape special characters that could be used for injection attacks.
    /// Escapes: {}, [], $, backticks, etc.
    /// </summary>
    EscapeSpecialChars = 2,

    /// <summary>
    /// Remove special characters entirely instead of escaping.
    /// </summary>
    RemoveSpecialChars = 3,

    /// <summary>
    /// Strict mode - only allow alphanumeric, basic punctuation, and whitespace.
    /// Removes everything else.
    /// </summary>
    Strict = 4,

    /// <summary>
    /// Custom sanitization using user-provided rules.
  /// </summary>
    Custom = 99
}

/// <summary>
/// Defines validation rules for prompt input.
/// </summary>
public enum PromptValidationLevel
{
    /// <summary>
    /// No validation - accept all input.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic validation - check length and non-empty.
/// </summary>
    Basic = 1,

    /// <summary>
 /// Moderate validation - check for obvious injection patterns.
    /// </summary>
 Moderate = 2,

    /// <summary>
    /// Strict validation - reject suspicious patterns and enforce content rules.
    /// </summary>
    Strict = 3
}

/// <summary>
/// Configuration options for prompt sanitization and validation.
/// </summary>
public sealed record PromptSanitizationOptions
{
    /// <summary>
    /// Sanitization mode to apply.
    /// </summary>
    public PromptSanitizationMode Mode { get; init; } = PromptSanitizationMode.Basic;

    /// <summary>
    /// Validation level to enforce.
    /// </summary>
    public PromptValidationLevel ValidationLevel { get; init; } = PromptValidationLevel.Basic;

    /// <summary>
    /// Maximum allowed prompt length in characters.
    /// </summary>
    public int MaxLength { get; init; } = 10000;

    /// <summary>
  /// Minimum required prompt length in characters.
    /// </summary>
    public int MinLength { get; init; } = 1;

    /// <summary>
    /// Whether to normalize Unicode characters to ASCII equivalents.
    /// </summary>
 public bool NormalizeUnicode { get; init; } = false;

    /// <summary>
    /// Whether to trim leading/trailing whitespace.
    /// </summary>
    public bool TrimWhitespace { get; init; } = true;

    /// <summary>
    /// Whether to collapse multiple consecutive whitespace into single space.
    /// </summary>
    public bool CollapseWhitespace { get; init; } = true;

  /// <summary>
    /// Custom characters to allow (only used with Custom mode).
    /// </summary>
    public string? AllowedCharacters { get; init; }

    /// <summary>
    /// Custom characters to explicitly block.
    /// </summary>
    public string? BlockedCharacters { get; init; }

    /// <summary>
    /// Patterns that should trigger validation failure (regex patterns).
    /// </summary>
    public List<string>? SuspiciousPatterns { get; init; }

    /// <summary>
    /// Custom sanitization function (only used with Custom mode).
    /// </summary>
    public Func<string, string>? CustomSanitizer { get; init; }

    /// <summary>
    /// Whether to throw exception on validation failure or return sanitized result.
    /// </summary>
    public bool ThrowOnValidationFailure { get; init; } = false;

    /// <summary>
    /// Default options for production use.
    /// </summary>
    public static PromptSanitizationOptions Default => new()
    {
      Mode = PromptSanitizationMode.EscapeSpecialChars,
        ValidationLevel = PromptValidationLevel.Moderate,
      MaxLength = 10000,
        MinLength = 1,
        TrimWhitespace = true,
  CollapseWhitespace = true
    };

    /// <summary>
    /// Strict options for high-security scenarios.
    /// </summary>
  public static PromptSanitizationOptions Strict => new()
    {
        Mode = PromptSanitizationMode.Strict,
        ValidationLevel = PromptValidationLevel.Strict,
        MaxLength = 5000,
 MinLength = 1,
        TrimWhitespace = true,
        CollapseWhitespace = true,
        NormalizeUnicode = true,
        ThrowOnValidationFailure = true
    };

    /// <summary>
    /// Permissive options for development/testing.
    /// </summary>
    public static PromptSanitizationOptions Permissive => new()
    {
        Mode = PromptSanitizationMode.Basic,
    ValidationLevel = PromptValidationLevel.Basic,
      MaxLength = 50000,
        MinLength = 0,
        TrimWhitespace = true,
  CollapseWhitespace = false
    };

    /// <summary>
    /// No sanitization (dangerous - only for testing).
    /// </summary>
public static PromptSanitizationOptions None => new()
    {
  Mode = PromptSanitizationMode.None,
        ValidationLevel = PromptValidationLevel.None,
        MaxLength = int.MaxValue,
        MinLength = 0,
TrimWhitespace = false,
        CollapseWhitespace = false
    };
}
