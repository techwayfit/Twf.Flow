using Twf.Flow.Nodes.AI;
using Twf.Flow.Core.Secrets;
using NSubstitute;

namespace Twf.Flow.Tests.Nodes.AI;

/// <summary>
/// Tests for LlmConfig secret resolution.
/// Verifies backward compatibility and secret reference support.
/// </summary>
public class LlmConfigSecretTests
{
    [Fact]
    public async Task GetApiKeyAsync_PlainApiKey_Should_Return_Value()
    {
        // Arrange
        var config = new LlmConfig { ApiKey = "sk-plain-key-123" };

        // Act
        var result = await config.GetApiKeyAsync();

// Assert
        result.Should().Be("sk-plain-key-123");
    }

    [Fact]
    public async Task GetApiKeyAsync_EmptyApiKey_Should_Return_Empty()
    {
        // Arrange
        var config = new LlmConfig { ApiKey = "" };

// Act
        var result = await config.GetApiKeyAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiKeyAsync_ApiKeyReference_Preferred_Over_ApiKey()
    {
        // Arrange
     var mockProvider = Substitute.For<ISecretProvider>();
        mockProvider.GetSecretAsync("env:PREFERRED_KEY").Returns("preferred-value");
        
        var config = new LlmConfig 
   { 
 ApiKey = "sk-fallback-key",
    ApiKeyReference = SecretReference.FromReference("env:PREFERRED_KEY")
        };

        // Act
        var result = await config.GetApiKeyAsync(mockProvider);

    // Assert
        result.Should().Be("preferred-value");
await mockProvider.Received(1).GetSecretAsync("env:PREFERRED_KEY");
    }

    [Fact]
    public async Task GetApiKeyAsync_SecretReference_In_ApiKey_Should_Resolve()
 {
// Arrange
 var mockProvider = Substitute.For<ISecretProvider>();
        mockProvider.IsSecretReference("env:TEST_KEY").Returns(true);
    mockProvider.GetSecretAsync("env:TEST_KEY").Returns("resolved-value");
        
   var config = new LlmConfig { ApiKey = "env:TEST_KEY" };

   // Act
        var result = await config.GetApiKeyAsync(mockProvider);

// Assert
        result.Should().Be("resolved-value");
    }

    [Fact]
    public async Task GetApiKeyAsync_NoProvider_PlainKey_Should_Work()
    {
   // Arrange
     var config = new LlmConfig { ApiKey = "sk-test-key" };

   // Act
        var result = await config.GetApiKeyAsync(secretProvider: null);

  // Assert
     result.Should().Be("sk-test-key");
    }

    [Fact]
    public async Task OpenAISecure_Should_Use_ApiKeyReference()
    {
        // Arrange
      var apiKeyRef = SecretReference.FromReference("env:OPENAI_API_KEY");
        var config = LlmConfig.OpenAISecure(apiKeyRef, "gpt-4o");

   var mockProvider = Substitute.For<ISecretProvider>();
        mockProvider.GetSecretAsync("env:OPENAI_API_KEY").Returns("sk-resolved-key");

        // Act
        var resolvedKey = await config.GetApiKeyAsync(mockProvider);

        // Assert
        config.Provider.Should().Be(LlmProvider.OpenAI);
        config.Model.Should().Be("gpt-4o");
 config.ApiKeyReference.Should().NotBeNull();
        resolvedKey.Should().Be("sk-resolved-key");
    }

    [Fact]
    public async Task AnthropicSecure_Should_Use_ApiKeyReference()
    {
 // Arrange
        var apiKeyRef = SecretReference.FromReference("env:ANTHROPIC_API_KEY");
     var config = LlmConfig.AnthropicSecure(apiKeyRef, "claude-sonnet-4-20250514");

   var mockProvider = Substitute.For<ISecretProvider>();
    mockProvider.GetSecretAsync("env:ANTHROPIC_API_KEY").Returns("sk-anthropic-key");

// Act
 var resolvedKey = await config.GetApiKeyAsync(mockProvider);

        // Assert
        config.Provider.Should().Be(LlmProvider.Anthropic);
     config.Model.Should().Be("claude-sonnet-4-20250514");
        config.ApiKeyReference.Should().NotBeNull();
    resolvedKey.Should().Be("sk-anthropic-key");
    }

    [Fact]
    public async Task OpenAI_Factory_Should_Support_Plain_And_Reference()
    {
        // Arrange - Plain key
     var configPlain = LlmConfig.OpenAI("sk-plain-key", "gpt-4o");

// Assert - Plain
 (await configPlain.GetApiKeyAsync()).Should().Be("sk-plain-key");

        // Arrange - Reference string
        var mockProvider = Substitute.For<ISecretProvider>();
        mockProvider.IsSecretReference("env:API_KEY").Returns(true);
        mockProvider.GetSecretAsync("env:API_KEY").Returns("sk-env-key");
        
     var configRef = LlmConfig.OpenAI("env:API_KEY", "gpt-4o");

        // Assert - Reference
 (await configRef.GetApiKeyAsync(mockProvider)).Should().Be("sk-env-key");
  }

    [Fact]
    public void Ollama_Should_Not_Require_ApiKey()
    {
        // Arrange & Act
     var config = LlmConfig.Ollama("llama3.2");

        // Assert
   config.ApiKey.Should().BeEmpty();
        config.ApiKeyReference.Should().BeNull();
    }

    [Fact]
    public async Task GetApiKeyAsync_With_Default_Provider_Should_Work()
    {
        // Arrange
   var testVarName = "TEST_LLMCONFIG_VAR_" + Guid.NewGuid().ToString("N");
  Environment.SetEnvironmentVariable(testVarName, "test-env-value");

    try
 {
            var config = new LlmConfig { ApiKey = $"env:{testVarName}" };

 // Act - Should use DefaultSecretProvider internally
 var result = await config.GetApiKeyAsync(new DefaultSecretProvider());

            // Assert
   result.Should().Be("test-env-value");
}
        finally
   {
            Environment.SetEnvironmentVariable(testVarName, null);
      }
    }
}
