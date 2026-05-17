using Twf.Flow.Nodes.Control;

namespace Twf.Flow.Tests.Nodes;

public class TryCatchNodeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_Success_Route_When_Try_Workflow_Succeeds()
    {
        var node = new TryCatchNode(
            "TryCatch",
            tryBuilder: w => w.AddNode(TestNode.Success("TryStep", "value", "ok")),
            catchBuilder: w => w.AddNode(TestNode.Success("CatchStep", "recovered", true)));

        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("try_catch_route").Should().Be("success");
        result.Data.Get<bool>("try_success").Should().BeTrue();
        result.Data.Get<bool>("try_error").Should().BeFalse();
        result.Data.Get<string>("value").Should().Be("ok");
        result.Data.Has("recovered").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Execute_Catch_Workflow_When_Try_Fails()
    {
        var node = new TryCatchNode(
            "TryCatch",
            tryBuilder: w => w.AddNode(TestNode.Failure("FailStep")),
            catchBuilder: w => w.AddStep("CatchHandler", (data, ctx) =>
            {
                var output = data.Clone().Set("recovered", true);
                return Task.FromResult(output);
            }));

        var input = new WorkflowData().Set("request_id", "r-123");
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("try_catch_route").Should().Be("catch");
        result.Data.Get<bool>("try_success").Should().BeFalse();
        result.Data.Get<bool>("try_error").Should().BeTrue();
        result.Data.Get<bool>("recovered").Should().BeTrue();
        result.Data.Get<string>("caught_failed_node").Should().Be("FailStep");
        result.Data.Get<string>("caught_error_message").Should().Contain("intentionally failed");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Try_Fails_And_No_Catch_Workflow_Configured()
    {
        var node = new TryCatchNode(
            "TryCatchNoCatch",
            tryBuilder: w => w.AddNode(TestNode.Failure("FailStep")));

        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("no catch workflow is configured");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Catch_Workflow_Also_Fails()
    {
        var node = new TryCatchNode(
            "TryCatchDoubleFail",
            tryBuilder: w => w.AddNode(TestNode.Failure("FailTry")),
            catchBuilder: w => w.AddNode(TestNode.Failure("FailCatch")));

        var input = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);
        var result = await node.ExecuteAsync(input, context);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Catch workflow failed");
    }
}
