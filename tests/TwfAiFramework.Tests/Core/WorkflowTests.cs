using Twf.Flow.Core;
using Twf.Flow.Tests.Nodes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for Workflow builder and execution.
/// Covers sequential execution, branching, parallel execution, loops, error handling.
/// </summary>
public class WorkflowTests
{
  [Fact]
    public async Task Workflow_Should_Execute_Sequential_Nodes()
    {
      // Arrange
        var workflow = Workflow.Create("SequentialTest")
            .AddNode(TestNode.Success("Node1", "step1", "value1"))
          .AddNode(TestNode.Success("Node2", "step2", "value2"))
   .AddNode(TestNode.Success("Node3", "step3", "value3"));

        var initialData = new WorkflowData();

  // Act
var result = await workflow.RunAsync(initialData);

      // Assert
        result.IsSuccess.Should().BeTrue();
 result.Data.Get<string>("step1").Should().Be("value1");
   result.Data.Get<string>("step2").Should().Be("value2");
        result.Data.Get<string>("step3").Should().Be("value3");
        result.NodeResults.Should().HaveCount(3);
    }

    [Fact]
public async Task Workflow_Should_Stop_On_First_Failure()
    {
        // Arrange
    var workflow = Workflow.Create("FailureTest")
      .AddNode(TestNode.Success("Node1"))
            .AddNode(TestNode.Failure("FailNode"))
            .AddNode(TestNode.Success("Node3")); // Should not execute

    var initialData = new WorkflowData();

        // Act
     var result = await workflow.RunAsync(initialData);

        // Assert
        result.IsFailure.Should().BeTrue();
 result.FailedNodeName.Should().Be("FailNode");
        result.NodeResults.Should().HaveCount(2); // Only 2 nodes executed
    }

    [Fact]
    public async Task Workflow_Should_Continue_On_Errors_When_Configured()
    {
        // Arrange
        var workflow = Workflow.Create("ContinueOnErrorTest")
  .ContinueOnErrors()
   .AddNode(TestNode.Success("Node1"))
            .AddNode(TestNode.Failure("FailNode"))
            .AddNode(TestNode.Success("Node3")); // Should still execute

        var initialData = new WorkflowData();

        // Act
  var result = await workflow.RunAsync(initialData);

        // Assert - workflow completes despite failure
     result.IsSuccess.Should().BeTrue();
 result.NodeResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task Workflow_Should_Execute_Branch_True_Path()
 {
        // Arrange
    var workflow = Workflow.Create("BranchTest")
        .AddStep("SetCondition", (data, _) => 
        Task.FromResult(data.Set("condition", true)))
            .Branch(
        data => data.Get<bool>("condition"),
 trueBranch: w => w.AddNode(TestNode.Success("TrueNode", "branch", "true")),
      falseBranch: w => w.AddNode(TestNode.Success("FalseNode", "branch", "false"))
     );

        var initialData = new WorkflowData();

     // Act
        var result = await workflow.RunAsync(initialData);

    // Assert
 result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch").Should().Be("true");
    }

    [Fact]
    public async Task Workflow_Should_Execute_Branch_False_Path()
    {
        // Arrange
var workflow = Workflow.Create("BranchTest")
         .AddStep("SetCondition", (data, _) => 
       Task.FromResult(data.Set("condition", false)))
            .Branch(
      data => data.Get<bool>("condition"),
             trueBranch: w => w.AddNode(TestNode.Success("TrueNode", "branch", "true")),
     falseBranch: w => w.AddNode(TestNode.Success("FalseNode", "branch", "false"))
  );

        var initialData = new WorkflowData();

        // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
        result.IsSuccess.Should().BeTrue();
 result.Data.Get<string>("branch").Should().Be("false");
    }

  [Fact]
    public async Task Workflow_Should_Execute_Parallel_Nodes()
    {
// Arrange
   var workflow = Workflow.Create("ParallelTest")
       .Parallel(
       TestNode.Success("Parallel1", "result1", "A"),
          TestNode.Success("Parallel2", "result2", "B"),
     TestNode.Success("Parallel3", "result3", "C")
      );

        var initialData = new WorkflowData();

        // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("result1").Should().Be("A");
      result.Data.Get<string>("result2").Should().Be("B");
        result.Data.Get<string>("result3").Should().Be("C");
    }

    [Fact]
    public async Task Workflow_Should_Execute_ForEach_Loop()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };
        var workflow = Workflow.Create("LoopTest")
   .AddStep("SetItems", (data, _) => 
Task.FromResult(data.Set("items", items)))
 .ForEach(
              "items",
     "results",
                loop => loop.AddStep("Process", (data, _) =>
 {
    var item = data.GetString("__loop_item__")!;
        return Task.FromResult(data.Set("processed", item.ToUpper()));
        })
        );

        var initialData = new WorkflowData();

      // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var results = result.Data.Get<List<WorkflowData>>("results");
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task Workflow_Should_Call_OnComplete_Handler()
    {
     // Arrange
        WorkflowResult? capturedResult = null;
     var workflow = Workflow.Create("CompleteTest")
     .AddNode(TestNode.Success("Node1"))
.OnComplete(r => capturedResult = r);

        var initialData = new WorkflowData();

        // Act
   await workflow.RunAsync(initialData);

        // Assert
        capturedResult.Should().NotBeNull();
      capturedResult!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Workflow_Should_Call_OnError_Handler()
{
        // Arrange
    string? capturedError = null;
        var workflow = Workflow.Create("ErrorTest")
   .AddNode(TestNode.Failure("FailNode"))
            .OnError((err, _) => capturedError = err);

        var initialData = new WorkflowData();

   // Act
      await workflow.RunAsync(initialData);

      // Assert
        capturedError.Should().NotBeNull();
        capturedError.Should().Contain("intentionally failed");
    }

 [Fact]
    public async Task Workflow_Should_Handle_Cancellation()
    {
        // Arrange
   var cts = new CancellationTokenSource();
        var workflow = Workflow.Create("CancelTest")
   .AddNode(TestNode.Delay("Delay1", TimeSpan.FromSeconds(10)));

        var initialData = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance, cancellationToken: cts.Token);

     // Act
        var task = workflow.RunAsync(initialData, context);
        cts.Cancel();
        var result = await task;

   // Assert
result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Workflow_Should_Track_Execution_Time()
    {
    // Arrange
    var workflow = Workflow.Create("TimingTest")
        .AddNode(TestNode.Delay("Delay", TimeSpan.FromMilliseconds(100)))
   .AddNode(TestNode.Success("Node2"));

        var initialData = new WorkflowData();

        // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
        result.TotalDuration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public async Task Workflow_Should_Clone_Data_For_Parallel_Nodes()
    {
        // Arrange
        var workflow = Workflow.Create("ParallelCloneTest")
            .AddStep("SetInitial", (data, _) => 
      Task.FromResult(data.Set("shared", "initial")))
            .Parallel(
              TestNode.Success("P1", "shared", "modified1"),
      TestNode.Success("P2", "shared", "modified2")
            );

        var initialData = new WorkflowData();

        // Act
     var result = await workflow.RunAsync(initialData);

     // Assert - last parallel node's value wins
        result.Data.Get<string>("shared").Should().NotBeNullOrEmpty();
 }

    [Fact]
    public async Task Workflow_Name_Should_Be_Accessible()
    {
        // Arrange
        var workflow = Workflow.Create("NamedWorkflow");

        // Assert
        workflow.Name.Should().Be("NamedWorkflow");
}

    [Fact]
    public async Task Workflow_Should_Track_All_Node_Results()
    {
// Arrange
 var workflow = Workflow.Create("TrackingTest")
       .AddNode(TestNode.Success("Node1"))
          .AddNode(TestNode.Success("Node2"))
            .AddNode(TestNode.Success("Node3"));

 var initialData = new WorkflowData();

        // Act
      var result = await workflow.RunAsync(initialData);

  // Assert
   result.NodeResults.Should().HaveCount(3);
   result.NodeResults[0].NodeName.Should().Be("Node1");
        result.NodeResults[1].NodeName.Should().Be("Node2");
        result.NodeResults[2].NodeName.Should().Be("Node3");
    }

    [Fact]
    public async Task Workflow_Should_Generate_Execution_Report()
 {
        // Arrange
 var workflow = Workflow.Create("ReportTest")
          .AddNode(TestNode.Success("Node1"))
       .AddNode(TestNode.Success("Node2"));

  var initialData = new WorkflowData();

 // Act
  var result = await workflow.RunAsync(initialData);

// Assert
        result.Report.Should().NotBeNull();
      result.Report.WorkflowName.Should().Be("ReportTest");
   result.Report.NodeBreakdown.Should().HaveCount(2);
        result.Report.SuccessCount.Should().Be(2);
 result.Report.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task Workflow_AddStep_Should_Create_LambdaNode()
    {
      // Arrange
        var workflow = Workflow.Create("LambdaTest")
   .AddStep("CustomStep", (data, _) =>
            {
        var value = data.Get<int>("counter");
         return Task.FromResult(data.Set("counter", value + 1));
  });

        var initialData = WorkflowData.From("counter", 5);

      // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
result.IsSuccess.Should().BeTrue();
        result.Data.Get<int>("counter").Should().Be(6);
    }
}
