using Twf.Flow.Nodes.Control;

namespace Twf.Flow.Tests.Nodes;

public class BranchNodeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Execute_Case1_With_CaseInsensitive_Matching_By_Default()
    {
        var case1Workflow= Workflow.Create("case1").AddStep("case1_step", (data, ctx) =>
        {
            data.Set("case", "case1");
            return Task.FromResult(data);
        });
        var case2Workflow = Workflow.Create("case2").AddStep("case2_step", (data, ctx) =>
        {
            data.Set("case", "case2");
            return Task.FromResult(data);
        });
        
        var node = new BranchNode(
            "BranchByStatus",
            "status", 
            new KeyValuePair<string, Workflow> ("case1",case1Workflow),
            new KeyValuePair<string, Workflow> ("case2",case2Workflow)
            );

        var input = new WorkflowData().Set("status", "case1");
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch_status").Should().Be("success");
        result.Data.Get<string>("branch_route").Should().Be("case1");
        result.Data.Get<string>("case").Should().Be("case1");
    } 
    
    [Fact]
    public async Task ExecuteAsync_Should_Select_Default_When_CaseSensitive_And_Value_Differs_By_Casing()
    {
        var case1Workflow= Workflow.Create("case1").AddStep("case1_step", (data, ctx) =>
        {
            data.Set("case", "case1");
            return Task.FromResult(data);
        });
        var defaultWorkflow = Workflow.Create("default").AddStep("default_step", (data, ctx) =>
        {
            data.Set("case", "default");
            return Task.FromResult(data);
        });
        
        var node = new BranchNode(
            "BranchByStatus",
            "status", 
            new KeyValuePair<string, Workflow> ("case1",case1Workflow),
            new KeyValuePair<string, Workflow> ("default",defaultWorkflow)
        );

        var input = new WorkflowData().Set("status", "case2");
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch_status").Should().Be("success");
        result.Data.Get<string>("branch_route").Should().Be("default");
        result.Data.Get<string>("case").Should().Be("default");
    }


    [Fact]
    public async Task ExecuteAsync_Should_Select_Last_Matching_Case_When_Multiple_Cases_Have_Same_Value()
    {
        var case1Workflow= Workflow.Create("case1_wf").AddStep("case1_step", (data, ctx) =>
        {
            data.Set("case", "case1");
            return Task.FromResult(data);
        });
        var case2Workflow = Workflow.Create("case2_wf").AddStep("case2_step", (data, ctx) =>
        {
            data.Set("case", "case2");
            return Task.FromResult(data);
        });
        
        var node = new BranchNode(
            "BranchByStatus",
            "status", 
            new KeyValuePair<string, Workflow> ("case1",case1Workflow),
            new KeyValuePair<string, Workflow> ("case1",case2Workflow)
        );

        var input = new WorkflowData().Set("status", "case1");//Last Worklflow
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch_status").Should().Be("success");
        result.Data.Get<string>("branch_route").Should().Be("case2_wf");
        result.Data.Get<string>("case").Should().Be("case2");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Failure_When_ValueKey_Is_Missing()
    {
        var defaultWorkflow = Workflow.Create("default").AddStep("default_step", (data, ctx) =>
        {
            data.Set("case", "default");
            return Task.FromResult(data);
        });
        var node = new BranchNode(
            "BranchMissingValue",
            "missing_key",
            new KeyValuePair<string, Workflow> ("default",defaultWorkflow));
  
        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("branch_route_status").Should().Be("failure");
    }
}
