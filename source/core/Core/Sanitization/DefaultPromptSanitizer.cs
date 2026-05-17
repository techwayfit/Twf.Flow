using System.Text;
using System.Text.RegularExpressions;

namespace Twf.Flow.Core.Sanitization;

/// <summary>
/// Default implementation of IPromptSanitizer with comprehensive sanitization
/// and validation rules to prevent prompt injection and ensure input quality.
/// </summary>
public class DefaultPromptSanitizer : IPromptSanitizer
{
    // Common injection patterns to detect
    private static readonly string[] SuspiciousPatternsDefault =
    [
     @"ignore\s+(previous|all|above|prior)\s+instructions?",
        @"disregard\s+(previous|all|above|prior)\s+instructions?",
    @"forget\s+(previous|all|above|prior)\s+instructions?",
        @"new\s+instructions?:",
 @"system\s*:",
        @"<\s*script\s*>",
        @"javascript\s*:",
        @"on(load|error|click)\s*=",
 @"\{\{.*eval.*\}\}",
        @"exec\s*\(",
        @"__import__\s*\(",
        @"os\.(system|popen|exec)",
    ];

    // Special characters that might be used for injection
    private static readonly char[] SpecialCharsToEscape =
        ['%', '{', '}', '[', ']', '$', '`', '\\', '<', '>'];

    // Strict mode: only allow these characters
    private static readonly Regex StrictAllowedCharsRegex = new(
        @"[^a-zA-Z0-9\s.,!?;:()\-'""@#]",
     RegexOptions.Compiled);

    /// <inheritdoc/>
    public string Sanitize(string prompt, PromptSanitizationOptions? options = null)
    {
        if (string.IsNullOrEmpty(prompt))
            return string.Empty;

        options ??= PromptSanitizationOptions.Default;

        // Validate first if configured
        if (options.ValidationLevel != PromptValidationLevel.None)
        {
            var (isValid, errors) = Validate(prompt, options);
            if (!isValid && options.ThrowOnValidationFailure)
            {
                throw new PromptValidationException(
                prompt,
                      "validation_failed",
            $"Prompt validation failed: {string.Join(", ", errors)}");
            }
        }

        var sanitized = prompt;

        // Apply sanitization based on mode
        sanitized = options.Mode switch
        {
            PromptSanitizationMode.None => sanitized,
            PromptSanitizationMode.Basic => ApplyBasicSanitization(sanitized, options),
            PromptSanitizationMode.EscapeSpecialChars => ApplyEscapeSanitization(sanitized, options),
            PromptSanitizationMode.RemoveSpecialChars => ApplyRemoveSanitization(sanitized, options),
            PromptSanitizationMode.Strict => ApplyStrictSanitization(sanitized, options),
            PromptSanitizationMode.Custom => ApplyCustomSanitization(sanitized, options),
            _ => sanitized
        };

        // Apply common transformations
        if (options.TrimWhitespace)
            sanitized = sanitized.Trim();

        if (options.CollapseWhitespace)
            sanitized = CollapseWhitespace(sanitized);

        if (options.NormalizeUnicode)
            sanitized = NormalizeUnicode(sanitized);

        // Final length check
        if (sanitized.Length > options.MaxLength)
            sanitized = sanitized[..options.MaxLength];

        return sanitized;
    }

    /// <inheritdoc/>
    public (bool IsValid, List<string> Errors) Validate(
          string prompt,
   PromptSanitizationOptions? options = null)
    {
        options ??= PromptSanitizationOptions.Default;
        var errors = new List<string>();

        // Basic validation
        if (options.ValidationLevel >= PromptValidationLevel.Basic)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                errors.Add("Prompt cannot be empty or whitespace");

            if (prompt.Length < options.MinLength)
                errors.Add($"Prompt too short (min: {options.MinLength} chars)");

            if (prompt.Length > options.MaxLength)
                errors.Add($"Prompt too long (max: {options.MaxLength} chars)");
        }

        // Moderate validation - check for injection patterns
        if (options.ValidationLevel >= PromptValidationLevel.Moderate)
        {
            if (ContainsSuspiciousPatterns(prompt))
                errors.Add("Prompt contains suspicious patterns (possible injection attempt)");

            // Check for excessive special characters (might indicate obfuscation)
            var specialCharCount = prompt.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
            var specialCharRatio = (double)specialCharCount / prompt.Length;
            if (specialCharRatio > 0.3)
                errors.Add($"Excessive special characters ({specialCharRatio:P0})");
        }

        // Strict validation
        if (options.ValidationLevel >= PromptValidationLevel.Strict)
        {
            // Check for control characters
            if (prompt.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
                errors.Add("Prompt contains control characters");

            // Check for null bytes
            if (prompt.Contains('\0'))
                errors.Add("Prompt contains null bytes");

            // Check blocked characters
            if (options.BlockedCharacters != null)
            {
                var blockedFound = options.BlockedCharacters.Where(prompt.Contains).ToList();
                if (blockedFound.Count != 0)
                    errors.Add($"Prompt contains blocked characters: {string.Join(", ", blockedFound)}");
            }

            // Check custom patterns
            if (options.SuspiciousPatterns != null)
            {
                foreach (var pattern in options.SuspiciousPatterns)
                {
                    try
                    {
                        if (Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase))
                            errors.Add($"Prompt matches blocked pattern: {pattern}");
                    }
                    catch (Exception)
                    {
                        // Invalid regex pattern - skip
                    }
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <inheritdoc/>
    public bool ContainsSuspiciousPatterns(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return false;

        foreach (var pattern in SuspiciousPatternsDefault)
        {
            try
            {
                if (Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            catch (Exception)
            {
                // Invalid pattern - skip
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public int EstimateTokenCount(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
            return 0;

        // Simple estimation: ~1 token per 4 characters (rough OpenAI approximation)
        // More accurate would use tiktoken or similar, but this is good enough
        var charCount = prompt.Length;
        var wordCount = prompt.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

        // Average between character-based and word-based estimates
        var charEstimate = charCount / 4;
        var wordEstimate = (int)(wordCount * 1.3); // Words are ~1.3 tokens on average

        return (charEstimate + wordEstimate) / 2;
    }

    // ─── Private Sanitization Methods ────────────────────────────────────────

    private static string ApplyBasicSanitization(string prompt, PromptSanitizationOptions options)
    {
        var sb = new StringBuilder(prompt.Length);

        foreach (var c in prompt)
        {
            // Remove control characters except newlines and tabs
            if (char.IsControl(c))
            {
                if (c == '\n' || c == '\r' || c == '\t')
                    sb.Append(c);
                // Skip other control chars
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string ApplyEscapeSanitization(string prompt, PromptSanitizationOptions options)
    {
        var result = ApplyBasicSanitization(prompt, options);
        var sb = new StringBuilder(result.Length + 100);

        foreach (var c in result)
        {
            if (SpecialCharsToEscape.Contains(c))
            {
                // Escape special characters
                sb.Append('\\').Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string ApplyRemoveSanitization(string prompt, PromptSanitizationOptions options)
    {
        var result = ApplyBasicSanitization(prompt, options);
        var sb = new StringBuilder(result.Length);

        foreach (var c in result)
        {
            if (!SpecialCharsToEscape.Contains(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string ApplyStrictSanitization(string prompt, PromptSanitizationOptions options)
    {
        var result = ApplyBasicSanitization(prompt, options);
        return StrictAllowedCharsRegex.Replace(result, "");
    }

    private static string ApplyCustomSanitization(string prompt, PromptSanitizationOptions options)
    {
        if (options.CustomSanitizer == null)
            return ApplyBasicSanitization(prompt, options);

        try
        {
            return options.CustomSanitizer(prompt);
        }
        catch (Exception ex)
        {
            throw new PromptSanitizationException(
                   prompt,
                        "Custom sanitizer failed",
               ex);
        }
    }

    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        var lastWasWhitespace = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasWhitespace)
                {
                    sb.Append(' ');
                    lastWasWhitespace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasWhitespace = false;
            }
        }

        return sb.ToString();
    }

    private static string NormalizeUnicode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalize to NFD (decomposed) then remove diacritics
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = char.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
