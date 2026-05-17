using System.Text.RegularExpressions;

namespace Twf.Flow.Core.Secrets;

/// <summary>
/// Default implementation of ISecretProvider that supports multiple secret sources.
/// Supports environment variables, plain values, and future extensibility.
/// </summary>
/// <remarks>
/// Supported reference formats:
/// - "env:VARIABLE_NAME" → Environment.GetEnvironmentVariable("VARIABLE_NAME")
/// - "file:path/to/secret.txt" → Read from file (development only)
/// - Plain string → Returned as-is (backward compatibility)
/// 
/// Example usage:
/// <code>
/// var provider = new DefaultSecretProvider();
/// var apiKey = await provider.GetSecretAsync("env:OPENAI_API_KEY");
/// </code>
/// </remarks>
public class DefaultSecretProvider : ISecretProvider
{
    private static readonly Regex SecretReferenceRegex = new(
        @"^(?<provider>env|file):(?<path>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc/>
    public Task<string> GetSecretAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Secret reference cannot be null or empty.", nameof(reference));

        // Check if it's a secret reference
        var match = SecretReferenceRegex.Match(reference);
        if (!match.Success)
        {
            // Plain value - return as-is (backward compatibility)
            return Task.FromResult(reference);
        }

        var provider = match.Groups["provider"].Value.ToLowerInvariant();
        var path = match.Groups["path"].Value;

        return provider switch
        {
            "env" => GetEnvironmentVariableAsync(path, reference),
            "file" => GetFileSecretAsync(path, reference),
            _ => throw new NotSupportedException(
     $"Secret provider '{provider}' is not supported. Supported providers: env, file")
        };
    }

    /// <inheritdoc/>
    public async Task<string?> TryGetSecretAsync(string reference)
    {
        try
        {
            return await GetSecretAsync(reference);
        }
        catch (SecretNotFoundException)
        {
            return null;
        }
        catch (SecretAccessException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public bool IsSecretReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return SecretReferenceRegex.IsMatch(value);
    }

    private Task<string> GetEnvironmentVariableAsync(string variableName, string reference)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrEmpty(value))
        {
            throw new SecretNotFoundException(
            reference,
                  $"Environment variable '{variableName}' is not set or is empty.");
        }

        return Task.FromResult(value);
    }

    private async Task<string> GetFileSecretAsync(string filePath, string reference)
    {
        try
        {
            // Resolve relative paths
            var fullPath = Path.IsPathRooted(filePath)
          ? filePath
 : Path.Combine(Directory.GetCurrentDirectory(), filePath);

            if (!File.Exists(fullPath))
            {
                throw new SecretNotFoundException(
         reference,
              $"Secret file not found at path: '{fullPath}'");
            }

            var content = await File.ReadAllTextAsync(fullPath);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new SecretNotFoundException(
                     reference,
                       $"Secret file at '{fullPath}' is empty.");
            }

            // Trim whitespace and newlines
            return content.Trim();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SecretAccessException(
                        reference,
                $"Access denied to secret file: '{filePath}'",
                   ex);
        }
        catch (IOException ex)
        {
            throw new SecretAccessException(
                reference,
        $"Error reading secret file: '{filePath}'",
              ex);
        }
    }
}
