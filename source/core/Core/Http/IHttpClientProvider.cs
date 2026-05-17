namespace Twf.Flow.Core.Http;

/// <summary>
/// Provides HttpClient instances for making HTTP requests.
/// This abstraction enables dependency injection and testability.
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
  /// Gets an HttpClient instance, optionally configured with a base URL.
    /// </summary>
    /// <param name="baseUrl">Optional base URL for the client. If provided, sets the BaseAddress property.</param>
    /// <returns>An HttpClient instance ready for use.</returns>
    /// <remarks>
    /// The returned HttpClient may be pooled or created fresh depending on the implementation.
    /// Callers should not dispose the returned client - the provider manages lifecycle.
    /// </remarks>
    HttpClient GetClient(string? baseUrl = null);
}
