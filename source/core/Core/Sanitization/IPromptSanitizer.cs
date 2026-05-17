namespace Twf.Flow.Core.Sanitization;

/// <summary>
/// Provides prompt sanitization and validation to prevent injection attacks
/// and ensure input quality before sending to LLMs.
/// </summary>
public interface IPromptSanitizer
{
    /// <summary>
    /// Sanitizes and validates a prompt according to configured rules.
    /// </summary>
    /// <param name="prompt">The raw prompt input.</param>
    /// <param name="options">Optional sanitization options. If null, uses default options.</param>
 /// <returns>Sanitized prompt ready for LLM consumption.</returns>
    /// <exception cref="PromptValidationException">
    /// Thrown when validation fails and ThrowOnValidationFailure is true.
    /// </exception>
  string Sanitize(string prompt, PromptSanitizationOptions? options = null);

    /// <summary>
  /// Validates a prompt without sanitizing it.
    /// </summary>
    /// <param name="prompt">The prompt to validate.</param>
    /// <param name="options">Validation options.</param>
    /// <returns>
    /// A tuple containing:
    /// - IsValid: Whether the prompt passes validation
    /// - Errors: List of validation error messages (empty if valid)
    /// </returns>
    (bool IsValid, List<string> Errors) Validate(string prompt, PromptSanitizationOptions? options = null);

    /// <summary>
    /// Checks if a prompt contains suspicious patterns that might indicate injection attempts.
    /// </summary>
  /// <param name="prompt">The prompt to check.</param>
    /// <returns>True if suspicious patterns are detected.</returns>
    bool ContainsSuspiciousPatterns(string prompt);

    /// <summary>
    /// Estimates the token count for a prompt (approximate).
    /// </summary>
    /// <param name="prompt">The prompt to estimate.</param>
    /// <returns>Approximate token count.</returns>
    int EstimateTokenCount(string prompt);
}
