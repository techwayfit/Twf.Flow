namespace Twf.Flow.Core.Sanitization;

/// <summary>
/// Exception thrown when prompt validation fails.
/// </summary>
public class PromptValidationException : Exception
{
    /// <summary>
    /// The original prompt that failed validation.
    /// </summary>
    public string OriginalPrompt { get; }

    /// <summary>
    /// The validation rule that was violated.
    /// </summary>
    public string ValidationRule { get; }

    /// <summary>
 /// Initializes a new instance of the <see cref="PromptValidationException"/> class.
    /// </summary>
  /// <param name="originalPrompt">The prompt that failed validation.</param>
    /// <param name="validationRule">The rule that was violated.</param>
    /// <param name="message">Error message.</param>
    public PromptValidationException(string originalPrompt, string validationRule, string message)
        : base(message)
    {
        OriginalPrompt = originalPrompt;
    ValidationRule = validationRule;
    }

    /// <summary>
    /// Initializes a new instance with an inner exception.
    /// </summary>
    public PromptValidationException(
   string originalPrompt, 
        string validationRule, 
        string message, 
        Exception innerException)
        : base(message, innerException)
    {
OriginalPrompt = originalPrompt;
        ValidationRule = validationRule;
    }
}

/// <summary>
/// Exception thrown when prompt sanitization encounters an error.
/// </summary>
public class PromptSanitizationException : Exception
{
  /// <summary>
    /// The original prompt being sanitized.
    /// </summary>
    public string OriginalPrompt { get; }

 /// <summary>
    /// Initializes a new instance of the <see cref="PromptSanitizationException"/> class.
    /// </summary>
    public PromptSanitizationException(string originalPrompt, string message)
        : base(message)
    {
        OriginalPrompt = originalPrompt;
    }

  /// <summary>
    /// Initializes a new instance with an inner exception.
    /// </summary>
    public PromptSanitizationException(string originalPrompt, string message, Exception innerException)
    : base(message, innerException)
    {
        OriginalPrompt = originalPrompt;
    }
}
