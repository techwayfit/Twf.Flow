using Twf.Flow.Nodes.Control;

namespace Twf.Flow.Tests.Nodes;

public class ErrorRouteNodeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Route_To_Error_When_Error_Message_Exists()
    {
        var node = new ErrorRouteNode();
        var input = new WorkflowData().Set("error_message", "Request failed");
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("error_route").Should().Be("error");
        result.Data.Get<bool>("route_error").Should().BeTrue();
        result.Data.Get<bool>("route_success").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Route_To_Error_When_Status_Code_Exceeds_Threshold()
    {
        var node = new ErrorRouteNode(errorStatusThreshold: 400);
        var input = new WorkflowData().Set("http_status_code", 500);
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("error_route").Should().Be("error");
        result.Data.Get<string>("routed_error_message").Should().Contain("HTTP status 500");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Route_To_Success_When_No_Error_Indicators_Exist()
    {
        var node = new ErrorRouteNode();
        var input = new WorkflowData().Set("payload", "ok");
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("error_route").Should().Be("success");
        result.Data.Get<bool>("route_success").Should().BeTrue();
        result.Data.Get<bool>("route_error").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Custom_Keys_For_Error_Detection()
    {
        var node = new ErrorRouteNode(
            errorMessageKey: "last_error",
            statusCodeKey: "status",
            errorStatusThreshold: 500);

        var input = new WorkflowData()
            .Set("status", 503)
            .Set("last_error", "Gateway timeout");
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("error_route").Should().Be("error");
        result.Data.Get<string>("routed_error_message").Should().Be("Gateway timeout");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Treat_404_As_Error_When_Threshold_Is_500()
    {
        var node = new ErrorRouteNode(errorStatusThreshold: 500);
        var input = new WorkflowData().Set("http_status_code", 404);
        var context = new WorkflowContext("Test", NullLogger.Instance);

        var result = await node.ExecuteAsync(input, context);

        result.IsSuccess.Should().BeTrue();
        result.Data.Get<string>("error_route").Should().Be("success");
        result.Data.Get<bool>("route_success").Should().BeTrue();
    }
}
