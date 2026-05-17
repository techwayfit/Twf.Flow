using Twf.Flow.Core.Secrets;

namespace Twf.Flow.Tests.Core.Secrets;

/// <summary>
/// Tests for DefaultSecretProvider.
/// Verifies environment variable resolution, file reading, and error handling.
/// </summary>
public class DefaultSecretProviderTests
{
    [Fact]
    public async Task GetSecretAsync_PlainValue_Should_Return_AsIs()
    {
        // Arrange
        var provider = new DefaultSecretProvider();
        var plainValue = "sk-abc123def456";

        // Act
        var result = await provider.GetSecretAsync(plainValue);

    // Assert
   result.Should().Be(plainValue);
    }

    [Fact]
    public async Task GetSecretAsync_EnvironmentVariable_Should_Resolve()
    {
   // Arrange
        var provider = new DefaultSecretProvider();
        var testVarName = "TEST_SECRET_VAR_" + Guid.NewGuid().ToString("N");
      var testValue = "test-secret-value-123";
        
        // Set environment variable for test
 Environment.SetEnvironmentVariable(testVarName, testValue);

        try
        {
            // Act
            var result = await provider.GetSecretAsync($"env:{testVarName}");

            // Assert
  result.Should().Be(testValue);
 }
        finally
        {
        // Cleanup
   Environment.SetEnvironmentVariable(testVarName, null);
 }
    }

    [Fact]
    public async Task GetSecretAsync_MissingEnvironmentVariable_Should_Throw()
    {
 // Arrange
   var provider = new DefaultSecretProvider();
        var nonExistentVar = "NONEXISTENT_VAR_" + Guid.NewGuid().ToString("N");

      // Act
        Func<Task> act = async () => await provider.GetSecretAsync($"env:{nonExistentVar}");

   // Assert
     await act.Should().ThrowAsync<SecretNotFoundException>()
    .WithMessage($"*{nonExistentVar}*");
    }

    [Fact]
    public async Task GetSecretAsync_FileSecret_Should_Read_Content()
    {
        // Arrange
        var provider = new DefaultSecretProvider();
        var tempFile = Path.GetTempFileName();
        var secretContent = "file-secret-content-xyz";
        
        await File.WriteAllTextAsync(tempFile, secretContent);

     try
  {
   // Act
    var result = await provider.GetSecretAsync($"file:{tempFile}");

       // Assert
   result.Should().Be(secretContent);
   }
        finally
{
      // Cleanup
   if (File.Exists(tempFile))
        File.Delete(tempFile);
   }
    }

    [Fact]
    public async Task GetSecretAsync_MissingFile_Should_Throw()
    {
   // Arrange
 var provider = new DefaultSecretProvider();
   var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid() + ".txt");

        // Act
        Func<Task> act = async () => await provider.GetSecretAsync($"file:{nonExistentFile}");

        // Assert
     await act.Should().ThrowAsync<SecretNotFoundException>();
    }

    [Fact]
 public async Task TryGetSecretAsync_ExistingSecret_Should_Return_Value()
    {
  // Arrange
        var provider = new DefaultSecretProvider();
        var plainValue = "sk-test-key";

     // Act
        var result = await provider.TryGetSecretAsync(plainValue);

        // Assert
   result.Should().Be(plainValue);
    }

    [Fact]
    public async Task TryGetSecretAsync_MissingSecret_Should_Return_Null()
    {
        // Arrange
      var provider = new DefaultSecretProvider();
        var nonExistentVar = "MISSING_VAR_" + Guid.NewGuid().ToString("N");

   // Act
        var result = await provider.TryGetSecretAsync($"env:{nonExistentVar}");

  // Assert
    result.Should().BeNull();
  }

    [Fact]
 public void IsSecretReference_EnvironmentVariable_Should_Return_True()
    {
        // Arrange
  var provider = new DefaultSecretProvider();

 // Act & Assert
        provider.IsSecretReference("env:API_KEY").Should().BeTrue();
        provider.IsSecretReference("ENV:API_KEY").Should().BeTrue();
    }

    [Fact]
    public void IsSecretReference_FileReference_Should_Return_True()
  {
 // Arrange
var provider = new DefaultSecretProvider();

        // Act & Assert
   provider.IsSecretReference("file:./secret.txt").Should().BeTrue();
 provider.IsSecretReference("FILE:/path/to/secret").Should().BeTrue();
    }

    [Fact]
    public void IsSecretReference_PlainValue_Should_Return_False()
    {
 // Arrange
 var provider = new DefaultSecretProvider();

        // Act & Assert
        provider.IsSecretReference("sk-abc123").Should().BeFalse();
   provider.IsSecretReference("plain-api-key").Should().BeFalse();
provider.IsSecretReference("").Should().BeFalse();
        provider.IsSecretReference(null!).Should().BeFalse();
    }

  [Fact]
    public async Task GetSecretAsync_FileSecret_Should_Trim_Whitespace()
    {
        // Arrange
        var provider = new DefaultSecretProvider();
      var tempFile = Path.GetTempFileName();
        var secretContent = "  secret-with-whitespace  \n";
        
        await File.WriteAllTextAsync(tempFile, secretContent);

        try
 {
      // Act
 var result = await provider.GetSecretAsync($"file:{tempFile}");

  // Assert
     result.Should().Be("secret-with-whitespace");
      }
   finally
{
       // Cleanup
if (File.Exists(tempFile))
     File.Delete(tempFile);
        }
    }

[Fact]
  public async Task GetSecretAsync_EmptyReference_Should_Throw()
    {
   // Arrange
     var provider = new DefaultSecretProvider();

// Act
      Func<Task> act = async () => await provider.GetSecretAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSecretAsync_UnsupportedProvider_Should_Throw()
    {
        // Arrange
        var provider = new DefaultSecretProvider();

    // Act
     Func<Task> act = async () => await provider.GetSecretAsync("azure:keyvault/secret");

// Assert
        await act.Should().ThrowAsync<NotSupportedException>()
      .WithMessage("*not supported*");
    }
}
