using Twf.Flow.Core;
using Twf.Flow.Core.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Twf.Flow.Nodes;

namespace Twf.Flow.Tests.Core.Integration;

/// <summary>
/// Integration tests for the refactored SOLID architecture.
/// Tests the complete flow: Builder ? Structure ? Executor ? Result
/// </summary>
public class WorkflowIntegrationTests
{
    // ??? Basic Workflow Execution ?????????????????????????????????????????????

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Simple_Workflow()
    {
      // Arrange
  var structure = WorkflowBuilder.Create("IntegrationTest")
         .AddNode(new TestNode("Node1", data => data.Set("key1", "value1")))
          .AddNode(new TestNode("Node2", data => data.Set("key2", data.GetString("key1") + "_modified")))
      .Build();

   var executor = new WorkflowExecutor();
   var initialData = new WorkflowData();

   // Act
    var result = await executor.ExecuteAsync(structure, initialData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.GetString("key1").Should().Be("value1");
        result.Data.GetString("key2").Should().Be("value1_modified");
   result.NodeResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task Workflow_Facade_Should_Execute_Same_As_Builder()
  {
 // Arrange - Using facade (old API)
        var facadeResult = await Workflow.Create("FacadeTest")
    .AddNode(new TestNode("Node1", data => data.Set("result", "facade")))
    .RunAsync();

        // Arrange - Using builder directly (new API)
        var structure = WorkflowBuilder.Create("BuilderTest")
       .AddNode(new TestNode("Node1", data => data.Set("result", "builder")))
            .Build();

      var executor = new WorkflowExecutor();
        var builderResult = await executor.ExecuteAsync(structure);

        // Assert - Both should work identically
        facadeResult.IsSuccess.Should().BeTrue();
     builderResult.IsSuccess.Should().BeTrue();
  facadeResult.NodeResults.Should().HaveCount(1);
        builderResult.NodeResults.Should().HaveCount(1);
    }

  // ??? Branch Execution ?????????????????????????????????????????????????????

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Branch_True()
    {
// Arrange
      var structure = WorkflowBuilder.Create("BranchTest")
          .AddNode(new TestNode("Setup", data => data.Set("condition", true)))
         .Branch(
    data => data.Get<bool>("condition"),
trueBranch => trueBranch.AddNode(new TestNode("TrueNode", d => d.Set("result", "true"))),
       falseBranch => falseBranch.AddNode(new TestNode("FalseNode", d => d.Set("result", "false"))))
         .Build();

        var executor = new WorkflowExecutor();

   // Act
   var result = await executor.ExecuteAsync(structure);

  // Assert
result.IsSuccess.Should().BeTrue();
   result.Data.GetString("result").Should().Be("true");
  }

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Branch_False()
    {
   // Arrange
     var structure = WorkflowBuilder.Create("BranchTest")
            .AddNode(new TestNode("Setup", data => data.Set("condition", false)))
        .Branch(
                data => data.Get<bool>("condition"),
                trueBranch => trueBranch.AddNode(new TestNode("TrueNode", d => d.Set("result", "true"))),
   falseBranch => falseBranch.AddNode(new TestNode("FalseNode", d => d.Set("result", "false"))))
            .Build();

      var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

     // Assert
        result.IsSuccess.Should().BeTrue();
result.Data.GetString("result").Should().Be("false");
    }

    // ??? Parallel Execution ???????????????????????????????????????????????????

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Parallel_Nodes()
    {
        // Arrange
  var structure = WorkflowBuilder.Create("ParallelTest")
     .Parallel(
   new TestNode("Parallel1", data => data.Set("key1", "value1")),
              new TestNode("Parallel2", data => data.Set("key2", "value2")),
 new TestNode("Parallel3", data => data.Set("key3", "value3")))
     .Build();

        var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

  // Assert
result.IsSuccess.Should().BeTrue();
        result.Data.GetString("key1").Should().Be("value1");
        result.Data.GetString("key2").Should().Be("value2");
    result.Data.GetString("key3").Should().Be("value3");
   result.NodeResults.Should().HaveCount(3);
    }

    // ??? Loop Execution ???????????????????????????????????????????????????????

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Loop()
    {
    // Arrange
  var structure = WorkflowBuilder.Create("LoopTest")
      .AddNode(new TestNode("Setup", data => data.Set("items", new[] { "a", "b", "c" })))
            .ForEach(
        "items",
    "results",
     bodyBuilder => bodyBuilder.AddNode(new TestNode("ProcessItem", data =>
    {
       var item = data.Get<string>("__loop_item__");
 return data.Set("processed", item?.ToUpper());
        })))
       .Build();

        var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

     // Assert
   result.IsSuccess.Should().BeTrue();
    var results = result.Data.Get<List<WorkflowData>>("results");
  results.Should().HaveCount(3);
        results![0].GetString("processed").Should().Be("A");
        results[1].GetString("processed").Should().Be("B");
        results[2].GetString("processed").Should().Be("C");
    }

    // ??? Error Handling ???????????????????????????????????????????????????????

 [Fact]
    public async Task WorkflowBuilder_Should_Stop_On_First_Failure()
    {
        // Arrange
  var structure = WorkflowBuilder.Create("ErrorTest")
  .AddNode(new TestNode("Node1", data => data.Set("key1", "value1")))
            .AddNode(new TestNode("FailingNode", data => throw new Exception("Intentional failure")))
      .AddNode(new TestNode("Node3", data => data.Set("key3", "should_not_execute")))
            .Build();

        var executor = new WorkflowExecutor();

    // Act
        var result = await executor.ExecuteAsync(structure);

  // Assert
  result.IsSuccess.Should().BeFalse();
result.FailedNodeName.Should().Be("FailingNode");
        result.Data.Has("key1").Should().BeTrue();
        result.Data.Has("key3").Should().BeFalse(); // Should not execute
    }

    [Fact]
    public async Task WorkflowBuilder_Should_Continue_On_Errors_When_Configured()
    {
   // Arrange
        var structure = WorkflowBuilder.Create("ContinueOnErrorTest")
            .ContinueOnErrors()
       .AddNode(new TestNode("Node1", data => data.Set("key1", "value1")))
      .AddNode(new TestNode("FailingNode", data => throw new Exception("Intentional failure")))
         .AddNode(new TestNode("Node3", data => data.Set("key3", "executed")))
   .Build();

   var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

   // Assert
  result.IsSuccess.Should().BeTrue(); // Should complete despite failure
        result.Data.GetString("key1").Should().Be("value1");
      result.Data.GetString("key3").Should().Be("executed"); // Should execute
    }

    // ??? Callbacks ????????????????????????????????????????????????????????????

  [Fact]
    public async Task WorkflowBuilder_Should_Invoke_OnComplete_Callback()
 {
        // Arrange
      WorkflowResult? capturedResult = null;
      
        var structure = WorkflowBuilder.Create("CallbackTest")
   .AddNode(new TestNode("Node1", data => data.Set("key", "value")))
     .OnComplete(result => capturedResult = result)
       .Build();

        var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedResult.Should().NotBeNull();
  capturedResult!.WorkflowName.Should().Be("CallbackTest");
    }

    [Fact]
    public async Task WorkflowBuilder_Should_Invoke_OnError_Callback()
    {
    // Arrange
    string? capturedError = null;
     Exception? capturedException = null;
        
     var structure = WorkflowBuilder.Create("ErrorCallbackTest")
     .AddNode(new TestNode("FailingNode", data => throw new InvalidOperationException("Test error")))
       .OnError((error, ex) =>
            {
 capturedError = error;
     capturedException = ex;
})
  .Build();

 var executor = new WorkflowExecutor();

  // Act
        var result = await executor.ExecuteAsync(structure);

    // Assert
        result.IsSuccess.Should().BeFalse();
        capturedError.Should().NotBeNull();
     capturedException.Should().BeOfType<InvalidOperationException>();
    }

    // ??? Cancellation ?????????????????????????????????????????????????????????

    [Fact]
  public async Task WorkflowBuilder_Should_Handle_Cancellation()
{
     // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

      var structure = WorkflowBuilder.Create("CancellationTest")
    .AddNode(new TestNode("Node1", data => data.Set("key", "value")))
       .Build();

        var executor = new WorkflowExecutor();

   // Act
        var result = await executor.ExecuteAsync(structure, null, null, cts.Token);

 // Assert
   result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancel");
    }

    // ??? Complex Workflow ?????????????????????????????????????????????????????

    [Fact]
    public async Task WorkflowBuilder_Should_Execute_Complex_Workflow()
    {
        // Arrange - Complex workflow with branches, parallel, and loops
        var structure = WorkflowBuilder.Create("ComplexTest")
 .AddNode(new TestNode("Init", data => data
     .Set("items", new[] { 1, 2, 3 })
  .Set("condition", true)))
       
 // Branch
.Branch(
      data => data.Get<bool>("condition"),
     trueBranch => trueBranch
     .AddNode(new TestNode("BranchNode", d => d.Set("branch_result", "true_branch"))))
            
   // Parallel
.Parallel(
      new TestNode("P1", data => data.Set("p1", "parallel1")),
      new TestNode("P2", data => data.Set("p2", "parallel2")))
    
        // Loop
          .ForEach("items", "loop_results",
   bodyBuilder => bodyBuilder.AddNode(new TestNode("LoopBody", data =>
      {
              var item = data.Get<int>("__loop_item__");
           return data.Set("doubled", item * 2);
      })))
  
       .Build();

        var executor = new WorkflowExecutor();

        // Act
        var result = await executor.ExecuteAsync(structure);

 // Assert
  result.IsSuccess.Should().BeTrue();
   result.Data.GetString("branch_result").Should().Be("true_branch");
    result.Data.GetString("p1").Should().Be("parallel1");
        result.Data.GetString("p2").Should().Be("parallel2");
        
        var loopResults = result.Data.Get<List<WorkflowData>>("loop_results");
        loopResults.Should().HaveCount(3);
 loopResults![0].Get<int>("doubled").Should().Be(2);
        loopResults[1].Get<int>("doubled").Should().Be(4);
        loopResults[2].Get<int>("doubled").Should().Be(6);
    }

    // ??? Helper Test Node ?????????????????????????????????????????????????????

    private class TestNode : BaseNode
    {
        private readonly Func<WorkflowData, WorkflowData> _func;

     public override string Name { get; }
     public override string Category => "Test";
        public override string Description => $"Test node: {Name}";

        public TestNode(string name, Func<WorkflowData, WorkflowData> func)
    {
          Name = name;
            _func = func;
        }

        protected override Task<WorkflowData> RunAsync(
    WorkflowData input,
      WorkflowContext context,
   NodeExecutionContext nodeCtx)
        {
          return Task.FromResult(_func(input));
        }
    }
}
