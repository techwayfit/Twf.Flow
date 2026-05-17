using FluentAssertions;
using Twf.Flow.Core;
using Twf.Flow.Core.Exceptions;

namespace Twf.Flow.Tests.Core.Exceptions;

/// <summary>
/// Tests for workflow-specific exception classes.
/// </summary>
public class WorkflowExceptionsTests
{
    [Fact]
    public void WorkflowException_Should_Capture_Context()
    {
        // Arrange & Act
        var ex = new NodeExecutionException(
    "TestNode",
     "TestWorkflow",
    "Node failed",
       "AI",
  "run-123",
       TimeSpan.FromSeconds(2));

        // Assert
        ex.WorkflowName.Should().Be("TestWorkflow");
        ex.NodeName.Should().Be("TestNode");
        ex.RunId.Should().Be("run-123");
        ex.Message.Should().Be("Node failed");
    }

    [Fact]
    public void NodeExecutionException_Should_Include_Node_Details()
    {
        // Arrange & Act
   var ex = new NodeExecutionException(
            "LlmNode",
            "ChatBot",
     "API call failed",
       "AI",
         "run-456",
        TimeSpan.FromSeconds(5),
  new HttpRequestException("Timeout"));

        // Assert
        ex.NodeName.Should().Be("LlmNode");
        ex.NodeCategory.Should().Be("AI");
        ex.ExecutionDuration.Should().Be(TimeSpan.FromSeconds(5));
        ex.InnerException.Should().BeOfType<HttpRequestException>();
  }

    [Fact]
    public void NodeExecutionException_FromNodeResult_Should_Create_Exception()
    {
        // Arrange
   var result = NodeResult.Failure(
"FailedNode",
        new WorkflowData(),
     "Execution failed",
            new InvalidOperationException("Test error"),
       TimeSpan.FromMilliseconds(500),
 DateTime.UtcNow);

        // Act
        var ex = NodeExecutionException.FromNodeResult(result, "TestWorkflow", "run-789");

        // Assert
        ex.NodeName.Should().Be("FailedNode");
        ex.WorkflowName.Should().Be("TestWorkflow");
   ex.RunId.Should().Be("run-789");
        ex.Message.Should().Be("Execution failed");
        ex.ExecutionDuration.Should().Be(TimeSpan.FromMilliseconds(500));
        ex.InnerException.Should().BeOfType<InvalidOperationException>();
    }

 [Fact]
    public void WorkflowDataMissingKeyException_Should_Capture_Key_Details()
    {
        // Act
        var ex = new WorkflowDataMissingKeyException(
    "user_id",
            "UserLookup",
   "FetchUserNode",
  typeof(string),
    "run-001");

        // Assert
        ex.MissingKey.Should().Be("user_id");
    ex.WorkflowName.Should().Be("UserLookup");
        ex.NodeName.Should().Be("FetchUserNode");
        ex.ExpectedType.Should().Be(typeof(string));
        ex.RunId.Should().Be("run-001");
    ex.Message.Should().Contain("user_id");
        ex.Message.Should().Contain("FetchUserNode");
        ex.Message.Should().Contain("String");
    }

 [Fact]
    public void NodeConfigurationException_InvalidParameter_Should_Build_Message()
    {
        // Act
        var ex = NodeConfigurationException.InvalidParameter(
"LlmNode",
       "ChatBot",
            "temperature",
   3.0f,
         "Value must be between 0 and 2");

        // Assert
      ex.NodeName.Should().Be("LlmNode");
        ex.WorkflowName.Should().Be("ChatBot");
        ex.ParameterName.Should().Be("temperature");
   ex.InvalidValue.Should().Be(3.0f);
    ex.Message.Should().Contain("temperature");
        ex.Message.Should().Contain("3");
        ex.Message.Should().Contain("between 0 and 2");
    }

    [Fact]
    public void WorkflowDataTypeException_Should_Capture_Type_Mismatch()
    {
        // Act
        var ex = new WorkflowDataTypeException(
            "age",
            typeof(string),
            typeof(int),
   "UserWorkflow",
   "ValidationNode",
            "run-002");

        // Assert
        ex.Key.Should().Be("age");
    ex.ActualType.Should().Be(typeof(string));
        ex.RequestedType.Should().Be(typeof(int));
        ex.WorkflowName.Should().Be("UserWorkflow");
        ex.NodeName.Should().Be("ValidationNode");
        ex.Message.Should().Contain("age");
     ex.Message.Should().Contain("String");
        ex.Message.Should().Contain("Int32");
    }

    [Fact]
    public void WorkflowCancelledException_Should_Capture_Cancellation_Context()
    {
        // Act
        var ex = new WorkflowCancelledException(
      "LongRunningWorkflow",
 "SlowNode",
            "run-003");

        // Assert
        ex.WorkflowName.Should().Be("LongRunningWorkflow");
   ex.CurrentNodeName.Should().Be("SlowNode");
        ex.RunId.Should().Be("run-003");
     ex.Message.Should().Contain("cancelled");
    }

    [Fact]
    public void WorkflowTimeoutException_Should_Capture_Timeout_Details()
    {
        // Act
        var ex = new WorkflowTimeoutException(
            "TimeoutWorkflow",
          TimeSpan.FromSeconds(30),
  "HangingNode",
          "run-004");

        // Assert
     ex.WorkflowName.Should().Be("TimeoutWorkflow");
        ex.Timeout.Should().Be(TimeSpan.FromSeconds(30));
      ex.CurrentNodeName.Should().Be("HangingNode");
        ex.RunId.Should().Be("run-004");
        ex.Message.Should().Contain("30");
    }

    [Fact]
    public void RetryLimitExceededException_Should_Capture_Retry_Details()
    {
// Arrange
        var attempts = new List<Exception>
        {
            new InvalidOperationException("Attempt 1 failed"),
     new InvalidOperationException("Attempt 2 failed"),
            new InvalidOperationException("Attempt 3 failed")
   };

 // Act
        var ex = new RetryLimitExceededException(
          "RetryNode",
     "RetryWorkflow",
       3,
            attempts,
        "run-005");

        // Assert
        ex.NodeName.Should().Be("RetryNode");
        ex.WorkflowName.Should().Be("RetryWorkflow");
        ex.MaxRetries.Should().Be(3);
   ex.AttemptExceptions.Should().HaveCount(3);
        ex.InnerException.Should().Be(attempts[2]); // Last attempt
  ex.Message.Should().Contain("3 retry attempts");
    }

    [Fact]
    public void WorkflowException_ToString_Should_Include_Context()
    {
        // Arrange
var ex = new NodeExecutionException(
   "TestNode",
  "TestWorkflow",
            "Test error",
            runId: "run-123");

     // Act
      var str = ex.ToString();

  // Assert
  str.Should().Contain("[Workflow: TestWorkflow]");
        str.Should().Contain("[Node: TestNode]");
     str.Should().Contain("[RunId: run-123]");
        str.Should().Contain("Test error");
    }

    [Fact]
  public void WorkflowException_Without_NodeName_Should_Format_Correctly()
    {
        // Arrange
        var ex = new WorkflowCancelledException("TestWorkflow", runId: "run-789");

// Act
        var str = ex.ToString();

        // Assert
        str.Should().Contain("[Workflow: TestWorkflow]");
        str.Should().NotContain("[Node:");
      str.Should().Contain("[RunId: run-789]");
    }

    [Fact]
    public void WorkflowException_Without_RunId_Should_Format_Correctly()
    {
      // Arrange
        var ex = new WorkflowDataMissingKeyException(
            "missing_key",
  "TestWorkflow",
  "TestNode");

    // Act
        var str = ex.ToString();

        // Assert
        str.Should().Contain("[Workflow: TestWorkflow]");
        str.Should().Contain("[Node: TestNode]");
      str.Should().NotContain("[RunId:");
    }
}
