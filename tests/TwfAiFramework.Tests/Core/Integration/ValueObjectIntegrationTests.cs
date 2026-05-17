using FluentAssertions;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes.AI;
using Twf.Flow.Nodes.Data;

namespace Twf.Flow.Tests.Core.Integration;

/// <summary>
/// Integration tests to verify value objects work correctly with node configurations.
/// Tests backward compatibility and new type-safe APIs.
/// </summary>
public class ValueObjectIntegrationTests
{
    [Fact]
    public void LlmConfig_Should_Accept_Value_Objects()
    {
        // Act
   var config = new LlmConfig
     {
            Provider = LlmProvider.OpenAI,
            Model = "gpt-4o",
  ApiKey = "test-key",
   Temperature = Temperature.Creative,
      MaxTokens = TokenCount.Extended
        };

        // Assert
        config.Temperature.Value.Should().Be(1.0f);
        config.MaxTokens.Value.Should().Be(4096);
    }

    [Fact]
    public void LlmConfig_Should_Support_Custom_Value_Objects()
    {
        // Act
        var config = new LlmConfig
        {
            Temperature = Temperature.FromValue(0.85f),
    MaxTokens = TokenCount.FromValue(3000)
        };

        // Assert
        config.Temperature.Value.Should().Be(0.85f);
        config.MaxTokens.Value.Should().Be(3000);
    }

    [Fact]
  public void LlmConfig_Should_Have_Sensible_Defaults()
  {
        // Act
  var config = new LlmConfig();

     // Assert
        config.Temperature.Should().Be(Temperature.Balanced); // 0.7
   config.MaxTokens.Should().Be(TokenCount.Standard);    // 2048
    }

    [Fact]
    public void LlmConfig_Factory_Methods_Should_Use_Defaults()
    {
        // Act
        var openAiConfig = LlmConfig.OpenAI("test-key");
        var anthropicConfig = LlmConfig.Anthropic("test-key");

      // Assert
   openAiConfig.Temperature.Value.Should().Be(0.7f);
 openAiConfig.MaxTokens.Value.Should().Be(2048);
        anthropicConfig.Temperature.Value.Should().Be(0.7f);
        anthropicConfig.MaxTokens.Value.Should().Be(2048);
    }

 [Fact]
    public void LlmConfig_With_Expression_Should_Override_Values()
    {
     // Act
        var config = LlmConfig.OpenAI("test-key") with
        {
      Temperature = Temperature.Deterministic,
      MaxTokens = TokenCount.Short
        };

        // Assert
    config.Temperature.Value.Should().Be(0.0f);
        config.MaxTokens.Value.Should().Be(256);
    }

    [Fact]
    public void ChunkConfig_Should_Accept_Value_Objects()
    {
     // Act
        var config = new ChunkConfig
        {
            ChunkSize = ChunkSize.Large,
            Overlap = ChunkOverlap.High,
            Strategy = ChunkStrategy.Sentence
        };

        // Assert
        config.ChunkSize.Value.Should().Be(1000);
        config.Overlap.Value.Should().Be(100);
config.Strategy.Should().Be(ChunkStrategy.Sentence);
  }

    [Fact]
    public void ChunkConfig_Should_Support_Custom_Value_Objects()
    {
        // Act
      var config = new ChunkConfig
      {
            ChunkSize = ChunkSize.FromValue(750),
            Overlap = ChunkOverlap.FromValue(75)
        };

  // Assert
        config.ChunkSize.Value.Should().Be(750);
        config.Overlap.Value.Should().Be(75);
    }

    [Fact]
    public void ChunkConfig_Should_Have_Sensible_Defaults()
    {
        // Act
        var config = new ChunkConfig();

   // Assert
        config.ChunkSize.Should().Be(ChunkSize.Standard);      // 500
        config.Overlap.Should().Be(ChunkOverlap.Standard);     // 50
    }

    [Fact]
    public void Value_Objects_Should_Reject_Invalid_Values()
    {
   // Act & Assert
        var tempAction = () => new LlmConfig { Temperature = Temperature.FromValue(3.0f) };
   tempAction.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Temperature must be between 0.0 and 2.0*");

        var tokenAction = () => new LlmConfig { MaxTokens = TokenCount.FromValue(200000) };
    tokenAction.Should().Throw<ArgumentOutOfRangeException>()
 .WithMessage("*Token count must be between 1 and 128,000*");

        var chunkAction = () => new ChunkConfig { ChunkSize = ChunkSize.FromValue(20) };
        chunkAction.Should().Throw<ArgumentOutOfRangeException>()
      .WithMessage("*Chunk size must be between 50 and 10,000*");

        var overlapAction = () => new ChunkConfig { Overlap = ChunkOverlap.FromValue(600) };
        overlapAction.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Chunk overlap must be between 0 and 500*");
    }

    [Fact]
    public void Value_Objects_Should_Convert_Implicitly_To_Primitives()
    {
        // Arrange
        var config = new LlmConfig
        {
            Temperature = Temperature.FromValue(0.8f),
            MaxTokens = TokenCount.FromValue(1500)
    };

    // Act - implicit conversion
        float tempValue = config.Temperature;
     int tokenValue = config.MaxTokens;

        // Assert
   tempValue.Should().Be(0.8f);
        tokenValue.Should().Be(1500);
    }

    [Theory]
    [InlineData(0.0f, "Deterministic")]
    [InlineData(0.3f, "Focused")]
  [InlineData(0.7f, "Balanced")]
    [InlineData(1.0f, "Creative")]
    [InlineData(1.5f, "VeryCreative")]
    public void Temperature_Predefined_Constants_Should_Map_To_Use_Cases(float value, string name)
    {
        // Arrange
        var temp = name switch
    {
            "Deterministic" => Temperature.Deterministic,
  "Focused" => Temperature.Focused,
            "Balanced" => Temperature.Balanced,
            "Creative" => Temperature.Creative,
     "VeryCreative" => Temperature.VeryCreative,
            _ => throw new ArgumentException("Unknown preset")
        };

        // Assert
        temp.Value.Should().Be(value);
    }

    [Theory]
  [InlineData(256, "Short")]
    [InlineData(2048, "Standard")]
    [InlineData(4096, "Extended")]
    [InlineData(8000, "LongForm")]
    [InlineData(16000, "Large")]
    [InlineData(32000, "VeryLarge")]
    public void TokenCount_Predefined_Constants_Should_Map_To_Use_Cases(int value, string name)
    {
        // Arrange
    var count = name switch
        {
        "Short" => TokenCount.Short,
     "Standard" => TokenCount.Standard,
       "Extended" => TokenCount.Extended,
            "LongForm" => TokenCount.LongForm,
         "Large" => TokenCount.Large,
      "VeryLarge" => TokenCount.VeryLarge,
            _ => throw new ArgumentException("Unknown preset")
        };

        // Assert
     count.Value.Should().Be(value);
    }

    [Fact]
    public void Value_Objects_Should_Work_In_Record_With_Expressions()
    {
        // Arrange
        var baseConfig = new LlmConfig
 {
    Provider = LlmProvider.OpenAI,
            Model = "gpt-4o",
        ApiKey = "test",
 Temperature = Temperature.Balanced,
    MaxTokens = TokenCount.Standard
     };

  // Act - create variations using with expressions
        var focusedConfig = baseConfig with { Temperature = Temperature.Focused };
        var creativeConfig = baseConfig with { Temperature = Temperature.Creative, MaxTokens = TokenCount.Extended };

        // Assert
        baseConfig.Temperature.Value.Should().Be(0.7f);
        focusedConfig.Temperature.Value.Should().Be(0.3f);
        creativeConfig.Temperature.Value.Should().Be(1.0f);
        creativeConfig.MaxTokens.Value.Should().Be(4096);
    }

    [Fact]
  public void Value_Objects_Should_Be_Equal_With_Same_Values()
    {
        // Arrange
        var temp1 = Temperature.FromValue(0.7f);
        var temp2 = Temperature.Balanced;
        var token1 = TokenCount.FromValue(2048);
    var token2 = TokenCount.Standard;

        // Assert
        temp1.Should().Be(temp2);
        token1.Should().Be(token2);
    }

    [Fact]
    public void Value_Objects_Should_Work_In_Collections()
    {
        // Arrange
  var temperatures = new List<Temperature>
        {
    Temperature.Deterministic,
 Temperature.Focused,
    Temperature.Balanced,
    Temperature.Creative
 };

 var tokenCounts = new Dictionary<string, TokenCount>
    {
["quick"] = TokenCount.Short,
            ["standard"] = TokenCount.Standard,
         ["detailed"] = TokenCount.Extended
        };

        // Assert
     temperatures.Should().HaveCount(4);
   temperatures[0].Value.Should().Be(0.0f);
tokenCounts["standard"].Value.Should().Be(2048);
  }
}
