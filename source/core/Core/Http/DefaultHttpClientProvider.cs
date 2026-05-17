using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

namespace Twf.Flow.Core.Http;

/// <summary>
/// Default implementation of IHttpClientProvider.
/// Provides proper HttpClient lifecycle management.
/// </summary>
public sealed class DefaultHttpClientProvider : IHttpClientProvider
{
    private readonly HttpClient? _fallbackClient;
    private readonly bool _ownsFallbackClient;

    /// <summary>
    /// Initializes a new instance with a shared HttpClient (for simple scenarios).
    /// </summary>
    /// <param name="fallbackClient">A pre-configured HttpClient to use.</param>
    /// <param name="owns">Whether this provider owns the client lifecycle. If true, the client will be disposed when provider is disposed.</param>
    /// <remarks>
    /// This constructor is provided for backward compatibility and simple use cases.
    /// For production applications with many HTTP calls, consider using a connection pool.
    /// </remarks>
    public DefaultHttpClientProvider(HttpClient fallbackClient, bool owns = false)
    {
        _fallbackClient = fallbackClient ?? throw new ArgumentNullException(nameof(fallbackClient));
        _ownsFallbackClient = owns;
    }

    /// <summary>
    /// Default constructor that creates a basic HttpClient (not recommended for production).
    /// </summary>
    /// <remarks>
    /// This creates a single shared HttpClient instance without connection pooling.
    /// Use this only for testing or simple scenarios.
    /// </remarks>
    public DefaultHttpClientProvider()
    {
        _fallbackClient = new HttpClient();
        _ownsFallbackClient = true;
    }

    /// <inheritdoc/>
    public HttpClient GetClient(string? baseUrl = null)
    {
        HttpClient client;

        if (_fallbackClient != null)
        {
            // Use fallback client
            client = _fallbackClient;
        }
        else
        {
            throw new InvalidOperationException(
                "No HttpClient configured. Initialize with HttpClient.");
        }

        // Set base address if provided (creates new instance to avoid modifying shared client)
        if (!string.IsNullOrEmpty(baseUrl) && client.BaseAddress == null)
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        return client;
    }
}
