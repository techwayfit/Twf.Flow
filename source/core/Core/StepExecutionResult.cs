namespace Twf.Flow.Core;

internal sealed class StepExecutionResult
{
    public bool IsSuccess { get; private init; }
    public WorkflowData Data { get; private init; } = new();
    public IReadOnlyList<NodeResult> Results { get; private init; } = Array.Empty<NodeResult>();
    public string? FailedNodeName { get; private init; }
    public string? ErrorMessage { get; private init; }
    public Exception? Exception { get; private init; }

    public static StepExecutionResult Ok(WorkflowData data, IEnumerable<NodeResult> results) => new()
    {
        IsSuccess = true, Data = data, Results = results.ToList()
    };

    public static StepExecutionResult Fail(NodeResult failed, WorkflowData lastGoodData) => new()
    {
        IsSuccess = false,
        Data = lastGoodData,
        Results = new[] { failed },
        FailedNodeName = failed.NodeName,
        ErrorMessage = failed.ErrorMessage,
        Exception = failed.Exception
    };
}
