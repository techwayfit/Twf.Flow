using Twf.Flow.Core.Http;

namespace Twf.Flow.Tests.Core.Http;

/// <summary>
/// Tests for IHttpClientProvider implementations.
/// Verifies proper client management.
/// </summary>
public class HttpClientProviderTests
{
    [Fact]
    public void DefaultHttpClientProvider_WithHttpClient_Should_Return_Client()
    {
        // Arrange
        var sharedClient = new HttpClient();
        var provider = new DefaultHttpClientProvider(sharedClient);

        // Act
        var client = provider.GetClient();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeSameAs(sharedClient);
    }

    [Fact]
    public void DefaultHttpClientProvider_WithBaseUrl_Should_Set_BaseAddress()
    {
        // Arrange
        var sharedClient = new HttpClient();
        var provider = new DefaultHttpClientProvider(sharedClient);
        var baseUrl = "https://api.example.com";

        // Act
        var client = provider.GetClient(baseUrl);

        // Assert
        client.BaseAddress.Should().NotBeNull();
        client.BaseAddress!.ToString().Should().Be(baseUrl + "/");
    }

    [Fact]
    public void DefaultHttpClientProvider_WithFallbackClient_Should_Return_Same_Client()
    {
        // Arrange
        var fallbackClient = new HttpClient();
        var provider = new DefaultHttpClientProvider(fallbackClient);

        // Act
        var client1 = provider.GetClient();
        var client2 = provider.GetClient();

        // Assert
        client1.Should().BeSameAs(fallbackClient);
        client2.Should().BeSameAs(fallbackClient);
    }

    [Fact]
    public void DefaultHttpClientProvider_DefaultConstructor_Should_Create_Client()
    {
        // Arrange
        var provider = new DefaultHttpClientProvider();

        // Act
        var client = provider.GetClient();

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void DefaultHttpClientProvider_NullFallbackClient_Should_Throw()
    {
        // Act
        Action act = () => new DefaultHttpClientProvider((HttpClient)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fallbackClient");
    }

    [Fact]
    public void DefaultHttpClientProvider_Multiple_Calls_With_Same_BaseUrl_Should_Reuse_Client()
    {
        // Arrange
        var provider = new DefaultHttpClientProvider();
        var baseUrl = "https://api.example.com";

        // Act
        var client1 = provider.GetClient(baseUrl);
        var client2 = provider.GetClient(baseUrl);

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.BaseAddress.Should().Be(client2.BaseAddress);
    }
}
