namespace Twf.Flow.Tests.Nodes;

/// <summary>
/// Helper test node for testing BaseNode behavior and workflow execution.
/// </summary>
internal class TestNode : BaseNode
{
    private readonly Func<WorkflowData, WorkflowContext, NodeExecutionContext, Task<WorkflowData>>? _executeFunc;
    private readonly bool _shouldThrow;
    private readonly TimeSpan _delay;

    public override string Name { get; }
    public override string Category { get; }

    public int ExecutionCount { get; private set; }

    public TestNode(
        string name,
        string category = "Test",
        Func<WorkflowData, WorkflowContext, NodeExecutionContext, Task<WorkflowData>>? executeFunc = null,
        bool shouldThrow = false,
        TimeSpan delay = default)
    {
        Name = name;
        Category = category;
        _executeFunc = executeFunc;
        _shouldThrow = shouldThrow;
        _delay = delay;
    }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input,
        WorkflowContext context,
        NodeExecutionContext nodeCtx)
    {
        ExecutionCount++;

        if (_delay > TimeSpan.Zero)
            await Task.Delay(_delay, context.CancellationToken);

        if (_shouldThrow)
            throw new InvalidOperationException($"{Name} intentionally failed");

        if (_executeFunc != null)
            return await _executeFunc(input, context, nodeCtx);

        // Default: pass through with execution marker
        return input.Clone().Set($"{Name}_executed", true);
    }

    public static TestNode Success(string name = "TestNode", string outputKey = "output", object? outputValue = null)
    {
        return new TestNode(name, executeFunc: (input, _, _) =>
        {
            var result = input.Clone().Set(outputKey, outputValue ?? $"{name}_result");
            return Task.FromResult(result);
        });
    }

    public static TestNode Failure(string name = "FailNode", string errorMessage = "Test failure")
    {
        return new TestNode(name, shouldThrow: true);
    }

    public static TestNode Delay(string name = "DelayNode", TimeSpan delay = default)
    {
        return new TestNode(name, delay: delay);
    }

    public static TestNode Transform(string name, string inputKey, string outputKey, Func<object?, object?> transform)
    {
        return new TestNode(name, executeFunc: (input, _, _) =>
        {
            var value = input.Get<object>(inputKey);
            var transformed = transform(value);
            return Task.FromResult(input.Clone().Set(outputKey, transformed));
        });
    }
}

/// <summary>
/// Tests for BaseNode - the abstract base class for all nodes.
/// Covers execution flow, error handling, logging, metadata, and cancellation.
/// </summary>
public class BaseNodeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_Success_When_Node_Succeeds()
    {
        // Arrange
        var node = TestNode.Success("TestNode", "result", "success");
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        var result = await node.ExecuteAsync(data, context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.NodeName.Should().Be("TestNode");
        result.Status.Should().Be(NodeStatus.Success);
        result.Data.Get<string>("result").Should().Be("success");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Failure_When_Node_Throws()
    {
        // Arrange
        var node = TestNode.Failure("FailNode");
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        var result = await node.ExecuteAsync(data, context);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Status.Should().Be(NodeStatus.Failed);
        result.ErrorMessage.Should().Contain("intentionally failed");
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var node = TestNode.Delay("DelayNode", TimeSpan.FromSeconds(10));
        var data = new WorkflowData();
        var cts = new CancellationTokenSource();
        var context = new WorkflowContext("Test", NullLogger.Instance, cancellationToken: cts.Token);

        // Act
        var task = node.ExecuteAsync(data, context);
        cts.Cancel();
        var result = await task;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Status.Should().Be(NodeStatus.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Track_Execution_Time()
    {
        // Arrange
        var node = TestNode.Delay("DelayNode", TimeSpan.FromMilliseconds(100));
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        var result = await node.ExecuteAsync(data, context);

        // Assert
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(90));
        result.StartedAt.Should().BeBefore(result.CompletedAt);
    }

    [Fact]
    public async Task NodeExecutionContext_Should_Allow_Metadata_Collection()
    {
        // Arrange
        var node = new TestNode("MetadataNode", "Test", (input, context, nodeCtx) =>
        {
            nodeCtx.SetMetadata("tokens", 150);
            nodeCtx.SetMetadata("model", "gpt-4");
            return Task.FromResult(input);
        });
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        var result = await node.ExecuteAsync(data, context);

        // Assert
        result.Metadata.Should().ContainKey("tokens");
        result.Metadata["tokens"].Should().Be(150);
        result.Metadata.Should().ContainKey("model");
        result.Metadata["model"].Should().Be("gpt-4");
    }

    [Fact]
    public async Task NodeExecutionContext_Should_Collect_Logs()
    {
        // Arrange
        var node = new TestNode("LogNode", "Test", (input, context, nodeCtx) =>
        {
            nodeCtx.Log("Step 1 complete");
            nodeCtx.Log("Step 2 complete");
            return Task.FromResult(input);
        });
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        var result = await node.ExecuteAsync(data, context);

        // Assert
        result.Logs.Should().HaveCount(2);
        result.Logs[0].Should().Contain("Step 1 complete");
        result.Logs[1].Should().Contain("Step 2 complete");
    }

    [Fact]
    public async Task Node_Should_Execute_Multiple_Times()
    {
        // Arrange
        var node = TestNode.Success();
        var data = new WorkflowData();
        var context = new WorkflowContext("Test", NullLogger.Instance);

        // Act
        await node.ExecuteAsync(data, context);
        await node.ExecuteAsync(data, context);
        await node.ExecuteAsync(data, context);

        // Assert
        node.ExecutionCount.Should().Be(3);
    }

    [Fact]
    public async Task ToString_Should_Return_Category_And_Name()
    {
        // Arrange
        var node = new TestNode("MyNode", "AI");

        // Act
        var str = node.ToString();

        // Assert
        str.Should().Be("AI/MyNode");
    }
}
