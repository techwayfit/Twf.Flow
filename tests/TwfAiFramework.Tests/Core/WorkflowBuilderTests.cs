namespace Twf.Flow.Tests.Core;

/// <summary>
/// Tests for Workflow builder and execution.
/// Covers node registration, execution flow, error handling, branching, parallel execution, and loops.
/// </summary>
public class WorkflowBuilderTests
{
    [Fact]
    public async Task Simple_Workflow_Should_Execute_Nodes_In_Sequence()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
  .AddNode(TestNode.Success("Node1", "step1", "value1"))
            .AddNode(TestNode.Success("Node2", "step2", "value2"))
       .AddNode(TestNode.Success("Node3", "step3", "value3"));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("step1").Should().Be("value1");
        result.Data.Get<string>("step2").Should().Be("value2");
        result.Data.Get<string>("step3").Should().Be("value3");
        result.NodeResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task Workflow_Should_Stop_On_First_Failure_By_Default()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
.AddNode(TestNode.Success("Node1", "step1", "value1"))
        .AddNode(TestNode.Failure("FailNode"))
            .AddNode(TestNode.Success("Node3", "step3", "value3"));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.FailedNodeName.Should().Be("FailNode");
        result.Data.Get<string>("step1").Should().Be("value1");
        result.Data.Has("step3").Should().BeFalse(); // Node3 never executed
        result.NodeResults.Should().HaveCount(2); // Only Node1 and FailNode
    }

    [Fact]
    public async Task Workflow_With_Initial_Data_Should_Pass_To_First_Node()
    {
        // Arrange
        var initialData = new WorkflowData().Set("initial", "value");
        var workflow = Workflow.Create("TestWorkflow")
            .AddNode(TestNode.Transform("TransformNode", "initial", "output", v => $"transformed_{v}"));

        // Act
        var result = await workflow.RunAsync(initialData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("output").Should().Be("transformed_value");
    }

    [Fact]
    public async Task AddStep_Should_Add_Inline_Lambda_Node()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
       .AddStep("InlineStep", async (data, ctx) =>
      {
          await Task.Delay(10);
          return data.Clone().Set("inline", "executed");
      });

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("inline").Should().Be("executed");
        result.NodeResults.Should().HaveCount(1);
        result.NodeResults[0].NodeName.Should().Be("InlineStep");
    }

    [Fact]
    public async Task Node_With_RunCondition_False_Should_Skip()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
      .AddNode(TestNode.Success("ConditionalNode", "output", "value"),
 NodeOptions.WithCondition(data => data.Get<bool>("enabled")));

        var dataDisabled = new WorkflowData().Set("enabled", false);

        // Act
        var result = await workflow.RunAsync(dataDisabled);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.NodeResults.Should().HaveCount(1);
        result.NodeResults[0].Status.Should().Be(NodeStatus.Skipped);
        result.Data.Has("output").Should().BeFalse();
    }

    [Fact]
    public async Task Node_With_RunCondition_True_Should_Execute()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
            .AddNode(TestNode.Success("ConditionalNode", "output", "value"),
     NodeOptions.WithCondition(data => data.Get<bool>("enabled")));

        var dataEnabled = new WorkflowData().Set("enabled", true);

        // Act
        var result = await workflow.RunAsync(dataEnabled);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.NodeResults[0].Status.Should().Be(NodeStatus.Success);
        result.Data.Get<string>("output").Should().Be("value");
    }

    [Fact]
    public async Task Node_With_ContinueOnError_Should_Not_Stop_Workflow()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
            .AddNode(TestNode.Success("Node1", "step1", "value1"))
      .AddNode(TestNode.Failure("FailNode"), NodeOptions.Default.AndContinueOnError())
     .AddNode(TestNode.Success("Node3", "step3", "value3"));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("step1").Should().Be("value1");
        result.Data.Get<string>("step3").Should().Be("value3");
        result.NodeResults.Should().HaveCount(3);
        result.NodeResults[1].Status.Should().Be(NodeStatus.Failed); // FailNode failed
    }

    [Fact]
    public async Task Node_With_ContinueOnError_And_Fallback_Should_Use_Fallback_Data()
    {
        // Arrange
        var fallback = new WorkflowData()
   .Set("step1", "value1") // Preserve previous data
       .Set("fallback", "used");
  
        var workflow = Workflow.Create("TestWorkflow")
            .AddNode(TestNode.Success("Node1", "step1", "value1"))
       .AddNode(TestNode.Failure("FailNode"), NodeOptions.Default.AndContinueOnError(fallback))
            .AddNode(TestNode.Success("Node3", "step3", "value3"));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("step1").Should().Be("value1");
 result.Data.Get<string>("fallback").Should().Be("used");
        result.Data.Get<string>("step3").Should().Be("value3");
        result.NodeResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task Node_With_Timeout_Should_Complete_If_Within_Limit()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
                .AddNode(TestNode.Delay("DelayNode", TimeSpan.FromMilliseconds(50)),
               NodeOptions.WithTimeout(TimeSpan.FromSeconds(1)));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Branch_Should_Execute_True_Branch_When_Condition_True()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
     .Branch(
       data => data.Get<bool>("shouldBranchTrue"),
             trueBranch => trueBranch.AddNode(TestNode.Success("TrueNode", "branch", "true")),
            falseBranch => falseBranch.AddNode(TestNode.Success("FalseNode", "branch", "false"))
            );

        var data = new WorkflowData().Set("shouldBranchTrue", true);

        // Act
        var result = await workflow.RunAsync(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch").Should().Be("true");
    }

    [Fact]
    public async Task Branch_Should_Execute_False_Branch_When_Condition_False()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
    .Branch(
           data => data.Get<bool>("shouldBranchTrue"),
      trueBranch => trueBranch.AddNode(TestNode.Success("TrueNode", "branch", "true")),
    falseBranch => falseBranch.AddNode(TestNode.Success("FalseNode", "branch", "false"))
 );

        var data = new WorkflowData().Set("shouldBranchTrue", false);

        // Act
        var result = await workflow.RunAsync(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch").Should().Be("false");
    }

    [Fact]
    public async Task Branch_Without_False_Branch_Should_Skip_When_Condition_False()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
            .Branch(
     data => data.Get<bool>("shouldExecute"),
   trueBranch => trueBranch.AddNode(TestNode.Success("TrueNode", "executed", "true"))
            );

        var data = new WorkflowData().Set("shouldExecute", false);

        // Act
        var result = await workflow.RunAsync(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Has("executed").Should().BeFalse();
    }

    [Fact]
    public async Task Parallel_Should_Execute_Nodes_Concurrently()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
   .Parallel(
          TestNode.Success("Parallel1", "p1", "value1"),
    TestNode.Success("Parallel2", "p2", "value2"),
       TestNode.Success("Parallel3", "p3", "value3")
  );

        // Act
        var startTime = DateTime.UtcNow;
        var result = await workflow.RunAsync();
        var duration = DateTime.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("p1").Should().Be("value1");
        result.Data.Get<string>("p2").Should().Be("value2");
        result.Data.Get<string>("p3").Should().Be("value3");
        result.NodeResults.Should().HaveCount(3);
    }

    [Fact]
    public async Task Parallel_Should_Fail_If_Any_Node_Fails()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
            .Parallel(
            TestNode.Success("Parallel1", "p1", "value1"),
             TestNode.Failure("FailParallel"),
            TestNode.Success("Parallel3", "p3", "value3")
                    );

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ForEach_Should_Execute_Body_For_Each_Item()
    {
        // Arrange
        var items = new List<object> { "item1", "item2", "item3" };
        var data = new WorkflowData().Set("items", items);

        var workflow = Workflow.Create("TestWorkflow")
          .ForEach("items", "results", body =>
     {
         body.AddStep("ProcessItem", (loopData, ctx) =>
        {
            var item = loopData.Get<string>("__loop_item__");
            var index = loopData.Get<int>("__loop_index__");
            return Task.FromResult(loopData.Clone().Set("processed", $"{item}_processed_{index}"));
        });
     });

        // Act
        var result = await workflow.RunAsync(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var results = result.Data.Get<List<WorkflowData>>("results");
        results.Should().HaveCount(3);
        results![0].Get<string>("processed").Should().Be("item1_processed_0");
        results[1].Get<string>("processed").Should().Be("item2_processed_1");
        results[2].Get<string>("processed").Should().Be("item3_processed_2");
    }

    [Fact]
    public async Task OnComplete_Callback_Should_Execute_On_Success()
    {
        // Arrange
        WorkflowResult? capturedResult = null;
        var workflow = Workflow.Create("TestWorkflow")
        .AddNode(TestNode.Success())
    .OnComplete(r => capturedResult = r);

        // Act
        await workflow.RunAsync();

        // Assert
        capturedResult.Should().NotBeNull();
        capturedResult!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task OnError_Callback_Should_Execute_On_Failure()
    {
        // Arrange
        string? capturedError = null;
        var workflow = Workflow.Create("TestWorkflow")
            .AddNode(TestNode.Failure())
            .OnError((msg, ex) => capturedError = msg);

        // Act
        await workflow.RunAsync();

        // Assert
        capturedError.Should().NotBeNull();
        capturedError.Should().Contain("intentionally failed");
    }

    [Fact]
    public async Task Workflow_Should_Track_Total_Duration()
    {
        // Arrange
        var workflow = Workflow.Create("TestWorkflow")
     .AddNode(TestNode.Delay("Delay1", TimeSpan.FromMilliseconds(50)))
  .AddNode(TestNode.Delay("Delay2", TimeSpan.FromMilliseconds(50)));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.TotalDuration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public async Task Complex_Workflow_Should_Handle_All_Features()
    {
        // Arrange
        var workflow = Workflow.Create("ComplexWorkflow")
            .AddNode(TestNode.Success("Init", "initialized", true))
            .Branch(
        data => data.Get<bool>("initialized"),
         trueBranch => trueBranch
         .AddNode(TestNode.Success("ProcessA", "processedA", "A"))
           .Parallel(
         TestNode.Success("Parallel1", "p1", "v1"),
 TestNode.Success("Parallel2", "p2", "v2")
 )
            )
       .AddNode(TestNode.Success("Finalize", "finalized", true));

        // Act
        var result = await workflow.RunAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Get<bool>("initialized").Should().BeTrue();
        result.Data.Get<string>("processedA").Should().Be("A");
        result.Data.Get<string>("p1").Should().Be("v1");
        result.Data.Get<string>("p2").Should().Be("v2");
        result.Data.Get<bool>("finalized").Should().BeTrue();
    }
}
