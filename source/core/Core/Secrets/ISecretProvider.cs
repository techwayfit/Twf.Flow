namespace Twf.Flow.Core.Secrets;

/// <summary>
/// Provides access to sensitive configuration values (API keys, connection strings, etc.)
/// without storing them as plain text in code or configuration files.
/// </summary>
/// <remarks>
/// Implementations can retrieve secrets from various sources:
/// - Environment variables
/// - Azure Key Vault
/// - AWS Secrets Manager
/// - Configuration files (for development only)
/// - Custom secret stores
/// 
/// Secret references use a URI-like format:
/// - "env:OPENAI_API_KEY" ? Environment variable
/// - "keyvault:my-vault/secrets/openai-key" ? Azure Key Vault
/// - "aws:secretsmanager:openai-key" ? AWS Secrets Manager
/// - "file:./secrets/api-key.txt" ? File (development only)
/// - Plain string ? Returned as-is (backward compatibility)
/// </remarks>
public interface ISecretProvider
{
    /// <summary>
    /// Resolves a secret reference to its actual value.
    /// </summary>
    /// <param name="reference">
    /// The secret reference. Can be:
    /// - A reference string (e.g., "env:API_KEY")
    /// - A plain value (returned as-is for backward compatibility)
    /// </param>
    /// <returns>The resolved secret value.</returns>
    /// <exception cref="SecretNotFoundException">Thrown when the secret cannot be found.</exception>
    /// <exception cref="SecretAccessException">Thrown when access to the secret is denied.</exception>
    Task<string> GetSecretAsync(string reference);

    /// <summary>
    /// Tries to resolve a secret reference, returning null if not found.
    /// </summary>
    /// <param name="reference">The secret reference.</param>
/// <returns>The resolved secret value, or null if not found.</returns>
    Task<string?> TryGetSecretAsync(string reference);

    /// <summary>
    /// Checks if a given string is a secret reference (vs. a plain value).
    /// </summary>
/// <param name="value">The value to check.</param>
    /// <returns>True if the value is a secret reference format.</returns>
    bool IsSecretReference(string value);
}
