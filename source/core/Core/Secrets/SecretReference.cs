namespace Twf.Flow.Core.Secrets;

/// <summary>
/// Represents a secure reference to a secret value (e.g., API key).
/// The actual secret is resolved at runtime via ISecretProvider.
/// </summary>
/// <remarks>
/// This value object encapsulates either:
/// - A secret reference (e.g., "env:OPENAI_API_KEY")
/// - A plain value (for backward compatibility)
/// 
/// The actual secret value is never stored in this object - only the reference.
/// Use <see cref="ResolveAsync"/> to get the actual value at runtime.
/// 
/// Example usage:
/// <code>
/// var apiKey = SecretReference.FromReference("env:OPENAI_API_KEY");
/// var actualKey = await apiKey.ResolveAsync(secretProvider);
/// </code>
/// </remarks>
public sealed class SecretReference
{
    /// <summary>
    /// The reference string or plain value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Indicates whether this is a secret reference (true) or plain value (false).
    /// </summary>
    public bool IsReference { get; }

    private SecretReference(string value, bool isReference)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsReference = isReference;
    }

    /// <summary>
    /// Creates a SecretReference from a reference string (e.g., "env:API_KEY").
    /// </summary>
    /// <param name="reference">The secret reference string.</param>
    /// <returns>A new SecretReference instance.</returns>
    public static SecretReference FromReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Secret reference cannot be null or empty.", nameof(reference));

        // Determine if it's actually a reference or plain value
        var isRef = reference.Contains(':') &&
             (reference.StartsWith("env:", StringComparison.OrdinalIgnoreCase) ||
      reference.StartsWith("file:", StringComparison.OrdinalIgnoreCase));

        return new SecretReference(reference, isRef);
    }

    /// <summary>
    /// Creates a SecretReference from a plain value (backward compatibility).
    /// </summary>
    /// <param name="plainValue">The plain secret value.</param>
    /// <returns>A new SecretReference instance.</returns>
    public static SecretReference FromPlainValue(string plainValue)
    {
        if (string.IsNullOrWhiteSpace(plainValue))
            throw new ArgumentException("Plain value cannot be null or empty.", nameof(plainValue));

        return new SecretReference(plainValue, isReference: false);
    }

    /// <summary>
    /// Resolves the secret reference to its actual value.
    /// </summary>
    /// <param name="provider">The secret provider to use for resolution.</param>
    /// <returns>The resolved secret value.</returns>
    /// <exception cref="SecretNotFoundException">Thrown when the secret cannot be found.</exception>
    /// <exception cref="SecretAccessException">Thrown when access to the secret is denied.</exception>
    public async Task<string> ResolveAsync(ISecretProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        return await provider.GetSecretAsync(Value);
    }

    /// <summary>
    /// Tries to resolve the secret reference, returning null if resolution fails.
    /// </summary>
    /// <param name="provider">The secret provider to use for resolution.</param>
    /// <returns>The resolved secret value, or null if resolution fails.</returns>
    public async Task<string?> TryResolveAsync(ISecretProvider provider)
    {
        if (provider == null)
            return null;

        return await provider.TryGetSecretAsync(Value);
    }

    /// <summary>
    /// Implicit conversion from string to SecretReference.
    /// </summary>
    public static implicit operator SecretReference(string value)
=> FromReference(value);

    /// <summary>
    /// Returns the reference string (not the resolved value).
    /// </summary>
    public override string ToString()
  => IsReference ? $"SecretRef[{Value}]" : "SecretRef[plain]";

    public override bool Equals(object? obj)
        => obj is SecretReference other && Value == other.Value && IsReference == other.IsReference;

    public override int GetHashCode()
  => HashCode.Combine(Value, IsReference);
}
