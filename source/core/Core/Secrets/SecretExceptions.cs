namespace Twf.Flow.Core.Secrets;

/// <summary>
/// Exception thrown when a secret cannot be found in the secret store.
/// </summary>
public class SecretNotFoundException : Exception
{
    /// <summary>
    /// The secret reference that was not found.
    /// </summary>
    public string SecretReference { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretNotFoundException"/> class.
    /// </summary>
    /// <param name="secretReference">The secret reference that was not found.</param>
    public SecretNotFoundException(string secretReference)
           : base($"Secret not found: '{secretReference}'")
    {
        SecretReference = secretReference;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="secretReference">The secret reference that was not found.</param>
    /// <param name="message">Custom error message.</param>
    public SecretNotFoundException(string secretReference, string message)
        : base(message)
    {
        SecretReference = secretReference;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="secretReference">The secret reference that was not found.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SecretNotFoundException(string secretReference, string message, Exception innerException)
   : base(message, innerException)
    {
        SecretReference = secretReference;
    }
}

/// <summary>
/// Exception thrown when access to a secret is denied.
/// </summary>
public class SecretAccessException : Exception
{
    /// <summary>
    /// The secret reference that could not be accessed.
    /// </summary>
    public string SecretReference { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretAccessException"/> class.
    /// </summary>
    /// <param name="secretReference">The secret reference that could not be accessed.</param>
    public SecretAccessException(string secretReference)
        : base($"Access denied to secret: '{secretReference}'")
    {
        SecretReference = secretReference;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretAccessException"/> class with a custom message.
    /// </summary>
    /// <param name="secretReference">The secret reference that could not be accessed.</param>
    /// <param name="message">Custom error message.</param>
    public SecretAccessException(string secretReference, string message)
        : base(message)
    {
        SecretReference = secretReference;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretAccessException"/> class with an inner exception.
    /// </summary>
    /// <param name="secretReference">The secret reference that could not be accessed.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SecretAccessException(string secretReference, string message, Exception innerException)
        : base(message, innerException)
    {
        SecretReference = secretReference;
    }
}
