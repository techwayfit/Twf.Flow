namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for NodeResult - the result of executing a single node.
/// Covers success/failure states, timing, metadata, callbacks, and transformations.
/// </summary>
public class NodeResultTests
{
    [Fact]
    public void Success_Should_Create_Successful_Result()
    {
        // Arrange
        var data = new WorkflowData().Set("output", "value");
        var duration = TimeSpan.FromMilliseconds(100);
        var startedAt = DateTime.UtcNow;

        // Act
        var result = NodeResult.Success("TestNode", data, duration, startedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.NodeName.Should().Be("TestNode");
        result.Status.Should().Be(NodeStatus.Success);
        result.Data.Should().BeSameAs(data);
        result.Duration.Should().Be(duration);
        result.StartedAt.Should().Be(startedAt);
        result.CompletedAt.Should().Be(startedAt + duration);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Success_With_Metadata_Should_Store_Metadata()
    {
        // Arrange
        var data = new WorkflowData();
        var metadata = new Dictionary<string, object> { ["tokens"] = 150 };
        var logs = new List<string> { "Processing started", "Processing complete" };

        // Act
        var result = NodeResult.Success("TestNode", data, TimeSpan.Zero, DateTime.UtcNow, metadata, logs);

        // Assert
        result.Metadata.Should().ContainKey("tokens");
        result.Metadata["tokens"].Should().Be(150);
        result.Logs.Should().HaveCount(2);
        result.Logs[0].Should().Be("Processing started");
    }

    [Fact]
    public void Failure_Should_Create_Failed_Result()
    {
        // Arrange
        var data = new WorkflowData();
        var exception = new InvalidOperationException("Test error");
        var duration = TimeSpan.FromMilliseconds(50);
        var startedAt = DateTime.UtcNow;

        // Act
        var result = NodeResult.Failure("TestNode", data, "Test error", exception, duration, startedAt);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.NodeName.Should().Be("TestNode");
        result.Status.Should().Be(NodeStatus.Failed);
        result.ErrorMessage.Should().Be("Test error");
        result.Exception.Should().BeSameAs(exception);
        result.Duration.Should().Be(duration);
    }

    [Fact]
    public void Failure_With_Custom_Status_Should_Set_Status()
    {
        // Arrange
        var data = new WorkflowData();

        // Act
        var result = NodeResult.Failure("TestNode", data, "Timeout", null,
  TimeSpan.FromSeconds(30), DateTime.UtcNow, NodeStatus.TimedOut);

        // Assert
        result.Status.Should().Be(NodeStatus.TimedOut);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Skipped_Should_Create_Skipped_Result()
    {
        // Arrange
        var data = new WorkflowData();

        // Act
        var result = NodeResult.Skipped("TestNode", data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(NodeStatus.Skipped);
        result.Duration.Should().Be(TimeSpan.Zero);
        result.Data.Should().BeSameAs(data);
    }

    [Fact]
    public void OnSuccess_Should_Execute_Action_When_Successful()
    {
        // Arrange
        var data = new WorkflowData().Set("key", "value");
        var result = NodeResult.Success("TestNode", data, TimeSpan.Zero, DateTime.UtcNow);
        WorkflowData? capturedData = null;

        // Act
        result.OnSuccess(d => capturedData = d);

        // Assert
        capturedData.Should().BeSameAs(data);
    }

    [Fact]
    public void OnSuccess_Should_Not_Execute_Action_When_Failed()
    {
        // Arrange
        var data = new WorkflowData();
        var result = NodeResult.Failure("TestNode", data, "Error", null, TimeSpan.Zero, DateTime.UtcNow);
        var actionExecuted = false;

        // Act
        result.OnSuccess(_ => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_Should_Execute_Action_When_Failed()
    {
        // Arrange
        var exception = new Exception("Test");
        var result = NodeResult.Failure("TestNode", new WorkflowData(), "Error", exception, TimeSpan.Zero, DateTime.UtcNow);
        string? capturedMessage = null;
        Exception? capturedException = null;

        // Act
        result.OnFailure((msg, ex) =>
        {
            capturedMessage = msg;
            capturedException = ex;
        });

        // Assert
        capturedMessage.Should().Be("Error");
        capturedException.Should().BeSameAs(exception);
    }

    [Fact]
    public void OnFailure_Should_Not_Execute_Action_When_Successful()
    {
        // Arrange
        var result = NodeResult.Success("TestNode", new WorkflowData(), TimeSpan.Zero, DateTime.UtcNow);
        var actionExecuted = false;

        // Act
        result.OnFailure((_, _) => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
    }

    [Fact]
    public void Map_Should_Transform_Data_When_Successful()
    {
        // Arrange
        var data = new WorkflowData().Set("input", "value");
        var result = NodeResult.Success("TestNode", data, TimeSpan.FromMilliseconds(100), DateTime.UtcNow);

        // Act
        var mapped = result.Map(d => d.Clone().Set("output", "transformed"));

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Data.Get<string>("output").Should().Be("transformed");
        mapped.Data.Get<string>("input").Should().Be("value");
    }

    [Fact]
    public void Map_Should_Pass_Through_Failure()
    {
        // Arrange
        var result = NodeResult.Failure("TestNode", new WorkflowData(), "Error", null, TimeSpan.Zero, DateTime.UtcNow);

        // Act
        var mapped = result.Map(d => d.Clone().Set("output", "transformed"));

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.ErrorMessage.Should().Be("Error");
        mapped.Data.Has("output").Should().BeFalse(); // Transform not applied
    }

    [Fact]
    public void ToString_Success_Should_Return_Readable_Format()
    {
        // Arrange
        var result = NodeResult.Success("TestNode", new WorkflowData(), TimeSpan.FromMilliseconds(123), DateTime.UtcNow);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("TestNode");
        str.Should().Contain("Success");
        str.Should().Contain("123ms");
    }

    [Fact]
    public void ToString_Failure_Should_Return_Readable_Format()
    {
        // Arrange
        var result = NodeResult.Failure("TestNode", new WorkflowData(), "Test error", null,
  TimeSpan.FromMilliseconds(50), DateTime.UtcNow);

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("TestNode");
        str.Should().Contain("Failed");
        str.Should().Contain("Test error");
        str.Should().Contain("50ms");
    }

    [Fact]
    public void Fluent_Callbacks_Should_Chain()
    {
        // Arrange
        var result = NodeResult.Success("TestNode", new WorkflowData().Set("value", 42),
     TimeSpan.Zero, DateTime.UtcNow);
        var successCalled = false;
        var failureCalled = false;

        // Act
        result
      .OnSuccess(_ => successCalled = true)
 .OnFailure((_, _) => failureCalled = true)
     .Map(d => d.Clone());

        // Assert
        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
    }
}
