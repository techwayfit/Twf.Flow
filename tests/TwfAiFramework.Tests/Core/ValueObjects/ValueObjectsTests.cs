using FluentAssertions;
using Twf.Flow.Core.ValueObjects;

namespace Twf.Flow.Tests.Core.ValueObjects;

/// <summary>
/// Tests for Temperature value object - ensures validation and factory methods work correctly.
/// </summary>
public class TemperatureTests
{
    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    public void FromValue_Should_Accept_Valid_Range(float value)
    {
   // Act
        var temp = Temperature.FromValue(value);

     // Assert
        temp.Value.Should().Be(value);
    }

 [Theory]
    [InlineData(-0.1f)]
    [InlineData(-1.0f)]
    [InlineData(2.1f)]
    [InlineData(5.0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void FromValue_Should_Reject_Invalid_Range(float value)
    {
        // Act
      Action act = () => Temperature.FromValue(value);

        // Assert
   act.Should().Throw<ArgumentOutOfRangeException>()
        .WithMessage("*Temperature must be between 0.0 and 2.0*");
    }

    [Fact]
    public void Predefined_Constants_Should_Have_Correct_Values()
    {
    // Assert
        Temperature.Deterministic.Value.Should().Be(0.0f);
     Temperature.Focused.Value.Should().Be(0.3f);
        Temperature.Balanced.Value.Should().Be(0.7f);
   Temperature.Creative.Value.Should().Be(1.0f);
      Temperature.VeryCreative.Value.Should().Be(1.5f);
    }

    [Fact]
    public void Implicit_Conversion_To_Float_Should_Work()
    {
    // Arrange
        var temp = Temperature.Balanced;

// Act
        float value = temp;

        // Assert
        value.Should().Be(0.7f);
    }

    [Fact]
    public void Explicit_Conversion_From_Float_Should_Work()
    {
     // Act
        var temp = (Temperature)0.8f;

        // Assert
        temp.Value.Should().Be(0.8f);
    }

    [Fact]
    public void Explicit_Conversion_From_Float_Should_Validate()
    {
  // Act
        Action act = () => { var _ = (Temperature)3.0f; };

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_Should_Format_With_Two_Decimals()
    {
        // Arrange
  var temp = Temperature.FromValue(0.73f);

      // Act
        var str = temp.ToString();

    // Assert
     str.Should().Be("0.73");
 }

    [Fact]
    public void Equality_Should_Work_Correctly()
    {
        // Arrange
        var temp1 = Temperature.FromValue(0.7f);
     var temp2 = Temperature.FromValue(0.7f);
  var temp3 = Temperature.FromValue(0.8f);

        // Assert
        temp1.Should().Be(temp2);
        temp1.Should().NotBe(temp3);
        (temp1 == temp2).Should().BeTrue();
      (temp1 != temp3).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_Be_Consistent()
    {
        // Arrange
        var temp1 = Temperature.FromValue(0.7f);
        var temp2 = Temperature.FromValue(0.7f);

        // Assert
        temp1.GetHashCode().Should().Be(temp2.GetHashCode());
    }
}

/// <summary>
/// Tests for TokenCount value object - ensures validation and predefined constants work correctly.
/// </summary>
public class TokenCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(2048)]
    [InlineData(8000)]
    [InlineData(32000)]
    [InlineData(128000)]
 public void FromValue_Should_Accept_Valid_Range(int value)
    {
      // Act
        var count = TokenCount.FromValue(value);

        // Assert
        count.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(128001)]
    [InlineData(1000000)]
    public void FromValue_Should_Reject_Invalid_Range(int value)
    {
        // Act
        Action act = () => TokenCount.FromValue(value);

   // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Token count must be between 1 and 128,000*");
    }

    [Fact]
    public void Predefined_Constants_Should_Have_Correct_Values()
    {
        // Assert
        TokenCount.Short.Value.Should().Be(256);
        TokenCount.Standard.Value.Should().Be(2048);
        TokenCount.Extended.Value.Should().Be(4096);
   TokenCount.LongForm.Value.Should().Be(8000);
 TokenCount.Large.Value.Should().Be(16000);
        TokenCount.VeryLarge.Value.Should().Be(32000);
    }

    [Fact]
    public void Implicit_Conversion_To_Int_Should_Work()
    {
    // Arrange
        var count = TokenCount.Standard;

        // Act
        int value = count;

        // Assert
        value.Should().Be(2048);
    }

    [Fact]
    public void Explicit_Conversion_From_Int_Should_Work()
    {
        // Act
        var count = (TokenCount)1000;

        // Assert
        count.Value.Should().Be(1000);
    }

    [Fact]
    public void ToString_Should_Format_With_Thousands_Separator()
    {
        // Arrange
  var count = TokenCount.FromValue(16000);

        // Act
        var str = count.ToString();

        // Assert
        str.Should().Contain("16,000");
  str.Should().Contain("tokens");
    }

    [Fact]
    public void Equality_Should_Work_Correctly()
    {
        // Arrange
        var count1 = TokenCount.FromValue(2048);
        var count2 = TokenCount.Standard;
  var count3 = TokenCount.Extended;

        // Assert
        count1.Should().Be(count2);
    count1.Should().NotBe(count3);
    }
}

/// <summary>
/// Tests for ChunkSize and ChunkOverlap value objects.
/// </summary>
public class ChunkingValueObjectsTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(10000)]
  public void ChunkSize_Should_Accept_Valid_Range(int value)
    {
        // Act
        var size = ChunkSize.FromValue(value);

      // Assert
        size.Value.Should().Be(value);
    }

  [Theory]
    [InlineData(49)]
    [InlineData(0)]
    [InlineData(10001)]
    [InlineData(100000)]
    public void ChunkSize_Should_Reject_Invalid_Range(int value)
    {
        // Act
        Action act = () => ChunkSize.FromValue(value);

      // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Chunk size must be between 50 and 10,000*");
}

    [Fact]
    public void ChunkSize_Predefined_Constants_Should_Have_Correct_Values()
    {
      // Assert
        ChunkSize.Small.Value.Should().Be(200);
        ChunkSize.Standard.Value.Should().Be(500);
        ChunkSize.Large.Value.Should().Be(1000);
        ChunkSize.ExtraLarge.Value.Should().Be(2000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void ChunkOverlap_Should_Accept_Valid_Range(int value)
    {
        // Act
        var overlap = ChunkOverlap.FromValue(value);

        // Assert
        overlap.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(501)]
    [InlineData(1000)]
    public void ChunkOverlap_Should_Reject_Invalid_Range(int value)
    {
 // Act
        Action act = () => ChunkOverlap.FromValue(value);

    // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
     .WithMessage("*Chunk overlap must be between 0 and 500*");
    }

    [Fact]
    public void ChunkOverlap_Predefined_Constants_Should_Have_Correct_Values()
  {
        // Assert
  ChunkOverlap.None.Value.Should().Be(0);
        ChunkOverlap.Minimal.Value.Should().Be(25);
        ChunkOverlap.Standard.Value.Should().Be(50);
        ChunkOverlap.High.Value.Should().Be(100);
    }

    [Fact]
    public void ChunkSize_Implicit_Conversion_To_Int_Should_Work()
    {
        // Arrange
        var size = ChunkSize.Standard;

        // Act
        int value = size;

        // Assert
        value.Should().Be(500);
    }

    [Fact]
    public void ChunkOverlap_Implicit_Conversion_To_Int_Should_Work()
    {
      // Arrange
        var overlap = ChunkOverlap.Standard;

 // Act
      int value = overlap;

        // Assert
        value.Should().Be(50);
  }
}
