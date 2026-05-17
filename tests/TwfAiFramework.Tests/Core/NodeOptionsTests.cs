namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for NodeOptions - per-node execution configuration.
/// Covers retry behavior, timeouts, conditional skipping, error handling, and fluent builder pattern.
/// </summary>
public class NodeOptionsTests
{
    [Fact]
    public void Default_Should_Have_Expected_Values()
    {
   // Arrange & Act
        var options = NodeOptions.Default;

        // Assert
        options.MaxRetries.Should().Be(0);
        options.RetryDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.RetryOnExceptions.Should().BeEmpty();
        options.Timeout.Should().BeNull();
     options.RunCondition.Should().BeNull();
  options.ContinueOnError.Should().BeFalse();
        options.FallbackData.Should().BeNull();
    }

    [Fact]
    public void WithRetry_Should_Create_Options_With_Retry_Settings()
{
        // Act
    var options = NodeOptions.WithRetry(3);

        // Assert
      options.MaxRetries.Should().Be(3);
        options.RetryDelay.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void WithRetry_With_Custom_Delay_Should_Set_Delay()
    {
 // Act
   var options = NodeOptions.WithRetry(5, TimeSpan.FromSeconds(2));

        // Assert
 options.MaxRetries.Should().Be(5);
      options.RetryDelay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void WithTimeout_Should_Create_Options_With_Timeout()
    {
        // Act
        var options = NodeOptions.WithTimeout(TimeSpan.FromSeconds(30));

        // Assert
options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void WithCondition_Should_Create_Options_With_RunCondition()
    {
      // Arrange
        Func<WorkflowData, bool> condition = data => data.Get<bool>("enabled");

        // Act
        var options = NodeOptions.WithCondition(condition);

        // Assert
   options.RunCondition.Should().BeSameAs(condition);
    }

    [Fact]
    public void AndContinueOnError_Should_Set_ContinueOnError_Flag()
    {
        // Arrange
        var options = NodeOptions.Default;

   // Act
        var updated = options.AndContinueOnError();

        // Assert
        updated.ContinueOnError.Should().BeTrue();
        updated.FallbackData.Should().BeNull();
    }

    [Fact]
    public void AndContinueOnError_With_Fallback_Should_Set_FallbackData()
    {
        // Arrange
        var options = NodeOptions.Default;
        var fallback = new WorkflowData().Set("fallback", true);

        // Act
        var updated = options.AndContinueOnError(fallback);

   // Assert
   updated.ContinueOnError.Should().BeTrue();
    updated.FallbackData.Should().BeSameAs(fallback);
    }

    [Fact]
public void AndTimeout_Should_Update_Timeout()
    {
    // Arrange
     var options = NodeOptions.WithRetry(3);

   // Act
        var updated = options.AndTimeout(TimeSpan.FromSeconds(60));

        // Assert
  updated.Timeout.Should().Be(TimeSpan.FromSeconds(60));
     updated.MaxRetries.Should().Be(3); // Original value preserved
    }

    [Fact]
    public void AndRetry_Should_Update_Retry_Settings()
    {
        // Arrange
        var options = NodeOptions.WithTimeout(TimeSpan.FromSeconds(30));

        // Act
        var updated = options.AndRetry(5, TimeSpan.FromSeconds(3));

 // Assert
    updated.MaxRetries.Should().Be(5);
        updated.RetryDelay.Should().Be(TimeSpan.FromSeconds(3));
        updated.Timeout.Should().Be(TimeSpan.FromSeconds(30)); // Original value preserved
    }

    [Fact]
    public void AndRetry_Without_Delay_Should_Preserve_Existing_Delay()
    {
        // Arrange
        var options = NodeOptions.WithRetry(3, TimeSpan.FromSeconds(5));

        // Act
        var updated = options.AndRetry(7);

        // Assert
        updated.MaxRetries.Should().Be(7);
        updated.RetryDelay.Should().Be(TimeSpan.FromSeconds(5)); // Preserved
    }

    [Fact]
    public void Fluent_Chain_Should_Build_Complex_Options()
    {
     // Arrange
        var fallback = new WorkflowData().Set("default", "value");

        // Act
        var options = NodeOptions.WithRetry(3)
            .AndTimeout(TimeSpan.FromSeconds(30))
            .AndContinueOnError(fallback);

        // Assert
 options.MaxRetries.Should().Be(3);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.ContinueOnError.Should().BeTrue();
     options.FallbackData.Should().BeSameAs(fallback);
    }

    [Fact]
    public void With_Expression_Should_Create_New_Instance()
    {
        // Arrange
        var original = NodeOptions.WithRetry(3);

        // Act
        var modified = original.AndTimeout(TimeSpan.FromSeconds(30));

        // Assert
        modified.Should().NotBeSameAs(original); // Different instances
        original.Timeout.Should().BeNull(); // Original unchanged
        modified.Timeout.Should().Be(TimeSpan.FromSeconds(30));
}

    [Fact]
    public void ToString_Should_Return_Readable_Summary()
    {
      // Arrange
    var options = NodeOptions.WithRetry(3)
        .AndTimeout(TimeSpan.FromSeconds(30))
          .AndContinueOnError();

        // Act
        var str = options.ToString();

        // Assert
str.Should().Contain("NodeOptions");
      str.Should().Contain("Retry=3");
        str.Should().Contain("Timeout=30s");
 str.Should().Contain("ContinueOnError=True");
    }

    [Fact]
    public void Record_Equality_Should_Compare_By_Value()
    {
        // Arrange
        var options1 = NodeOptions.WithRetry(3).AndTimeout(TimeSpan.FromSeconds(30));
        var options2 = NodeOptions.WithRetry(3).AndTimeout(TimeSpan.FromSeconds(30));
    var options3 = NodeOptions.WithRetry(5).AndTimeout(TimeSpan.FromSeconds(30));

     // Assert
        options1.Should().Be(options2); // Same values = equal
        options1.Should().NotBe(options3); // Different values = not equal
    }
}
