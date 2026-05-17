using Twf.Flow.Core.Secrets;
using NSubstitute;

namespace Twf.Flow.Tests.Core.Secrets;

/// <summary>
/// Tests for SecretReference value object.
/// Verifies reference creation, resolution, and equality.
/// </summary>
public class SecretReferenceTests
{
    [Fact]
    public void FromReference_EnvironmentVariable_Should_Be_Reference()
    {
   // Act
        var secret = SecretReference.FromReference("env:API_KEY");

        // Assert
        secret.IsReference.Should().BeTrue();
        secret.Value.Should().Be("env:API_KEY");
    }

    [Fact]
    public void FromReference_FileReference_Should_Be_Reference()
    {
   // Act
    var secret = SecretReference.FromReference("file:./secret.txt");

        // Assert
  secret.IsReference.Should().BeTrue();
        secret.Value.Should().Be("file:./secret.txt");
 }

    [Fact]
 public void FromReference_PlainValue_Should_Not_Be_Reference()
    {
        // Act
        var secret = SecretReference.FromReference("sk-abc123");

// Assert
 secret.IsReference.Should().BeFalse();
        secret.Value.Should().Be("sk-abc123");
    }

    [Fact]
    public void FromPlainValue_Should_Create_Non_Reference()
    {
        // Act
        var secret = SecretReference.FromPlainValue("plain-key-123");

  // Assert
    secret.IsReference.Should().BeFalse();
    secret.Value.Should().Be("plain-key-123");
    }

[Fact]
    public void FromReference_NullOrEmpty_Should_Throw()
  {
        // Act & Assert
      Action act1 = () => SecretReference.FromReference(null!);
   Action act2 = () => SecretReference.FromReference("");
        Action act3 = () => SecretReference.FromReference("   ");

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
      act3.Should().Throw<ArgumentException>();
    }

[Fact]
    public void FromPlainValue_NullOrEmpty_Should_Throw()
 {
        // Act & Assert
        Action act1 = () => SecretReference.FromPlainValue(null!);
        Action act2 = () => SecretReference.FromPlainValue("");

        act1.Should().Throw<ArgumentException>();
    act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ResolveAsync_Should_Call_Provider()
    {
        // Arrange
  var mockProvider = Substitute.For<ISecretProvider>();
        mockProvider.GetSecretAsync("env:TEST_KEY").Returns("resolved-value");
        
        var secret = SecretReference.FromReference("env:TEST_KEY");

   // Act
   var result = await secret.ResolveAsync(mockProvider);

        // Assert
        result.Should().Be("resolved-value");
    await mockProvider.Received(1).GetSecretAsync("env:TEST_KEY");
    }

[Fact]
    public async Task TryResolveAsync_Success_Should_Return_Value()
    {
        // Arrange
  var mockProvider = Substitute.For<ISecretProvider>();
mockProvider.TryGetSecretAsync("env:TEST_KEY").Returns("resolved-value");
      
        var secret = SecretReference.FromReference("env:TEST_KEY");

        // Act
        var result = await secret.TryResolveAsync(mockProvider);

    // Assert
        result.Should().Be("resolved-value");
    }

    [Fact]
    public async Task TryResolveAsync_Failure_Should_Return_Null()
 {
        // Arrange
        var mockProvider = Substitute.For<ISecretProvider>();
mockProvider.TryGetSecretAsync("env:MISSING_KEY").Returns((string?)null);
        
        var secret = SecretReference.FromReference("env:MISSING_KEY");

// Act
        var result = await secret.TryResolveAsync(mockProvider);

     // Assert
   result.Should().BeNull();
    }

    [Fact]
    public async Task TryResolveAsync_NullProvider_Should_Return_Null()
    {
        // Arrange
        var secret = SecretReference.FromReference("env:TEST_KEY");

// Act
        var result = await secret.TryResolveAsync(null);

      // Assert
  result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_NullProvider_Should_Throw()
    {
        // Arrange
   var secret = SecretReference.FromReference("env:TEST_KEY");

        // Act
  Func<Task> act = async () => await secret.ResolveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void ImplicitConversion_From_String_Should_Work()
    {
   // Act
        SecretReference secret = "env:API_KEY";

   // Assert
        secret.Should().NotBeNull();
  secret.Value.Should().Be("env:API_KEY");
    }

    [Fact]
    public void ToString_Reference_Should_Show_Type()
    {
  // Arrange
  var secret = SecretReference.FromReference("env:API_KEY");

      // Act
        var str = secret.ToString();

   // Assert
     str.Should().Contain("SecretRef");
        str.Should().Contain("env:API_KEY");
    }

    [Fact]
    public void ToString_PlainValue_Should_Not_Expose_Value()
    {
      // Arrange
        var secret = SecretReference.FromPlainValue("sk-secret-key-123");

        // Act
   var str = secret.ToString();

  // Assert
str.Should().Contain("SecretRef");
        str.Should().NotContain("sk-secret-key-123"); // Security: don't expose plain value
    }

    [Fact]
    public void Equals_Same_Reference_Should_Be_True()
    {
   // Arrange
var secret1 = SecretReference.FromReference("env:API_KEY");
     var secret2 = SecretReference.FromReference("env:API_KEY");

   // Assert
        secret1.Equals(secret2).Should().BeTrue();
        (secret1 == secret2).Should().BeFalse(); // Different instances
    }

    [Fact]
    public void GetHashCode_Same_Reference_Should_Be_Equal()
    {
        // Arrange
   var secret1 = SecretReference.FromReference("env:API_KEY");
var secret2 = SecretReference.FromReference("env:API_KEY");

// Assert
     secret1.GetHashCode().Should().Be(secret2.GetHashCode());
    }
}
