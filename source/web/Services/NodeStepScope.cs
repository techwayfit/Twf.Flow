using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Services;

/// <summary>
/// Scoped helper that brackets a single node's execution with SSE step events.
///
/// Usage:
/// <code>
///   await using var scope = await NodeStepScope.StartAsync(nodeDef, inputData, hasIn, hasOut, onStep);
///   // ... execute ...
///   scope.Complete(outputData);   // or scope.Fail("reason")
/// </code>
///
/// <see cref="DisposeAsync"/> emits:
///   <list type="bullet">
///     <item><c>node_done</c>  — when <see cref="Complete"/> was called</item>
///     <item><c>node_error</c> — when <see cref="Fail"/> was called, or neither was called
///           (i.e. an exception escaped the using block)</item>
///   </list>
/// </summary>
internal sealed class NodeStepScope : IAsyncDisposable
{
    private enum Outcome { None, Done, Error }

    private readonly Func<NodeStepEvent, Task> _onStep;
    private readonly NodeDefinition             _nodeDef;
    private readonly Dictionary<string, object?> _inputData;
    private readonly bool _dataInConfigured;
    private readonly bool _dataOutConfigured;

    private Outcome _outcome = Outcome.None;
    private Dictionary<string, object?> _outputData = [];
    private string? _errorMessage;

    private NodeStepScope(
        NodeDefinition nodeDef,
        Dictionary<string, object?> inputData,
        bool dataInConfigured,
        bool dataOutConfigured,
        Func<NodeStepEvent, Task> onStep)
    {
        _nodeDef          = nodeDef;
        _inputData        = inputData;
        _dataInConfigured  = dataInConfigured;
        _dataOutConfigured = dataOutConfigured;
        _onStep           = onStep;
    }

    /// <summary>
    /// Emits <c>node_start</c> and returns a scope that will emit the terminal event on disposal.
    /// </summary>
    public static async Task<NodeStepScope> StartAsync(
        NodeDefinition nodeDef,
        Dictionary<string, object?> inputData,
        bool dataInConfigured,
        bool dataOutConfigured,
        Func<NodeStepEvent, Task> onStep)
    {
        var scope = new NodeStepScope(nodeDef, inputData, dataInConfigured, dataOutConfigured, onStep);
        await onStep(scope.MakeEvent("node_start", [], null));
        return scope;
    }

    /// <summary>Mark the node as successfully completed. Call before leaving the using block.</summary>
    public void Complete(Dictionary<string, object?> outputData)
    {
        _outputData = outputData;
        _outcome    = Outcome.Done;
    }

    /// <summary>Mark the node as failed. Call before leaving the using block on error paths.</summary>
    public void Fail(string errorMessage)
    {
        _errorMessage = errorMessage;
        _outcome      = Outcome.Error;
    }

    /// <summary>
    /// Emits the terminal SSE event. Automatically called at the end of the using block.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_outcome == Outcome.Done)
            await _onStep(MakeEvent("node_done", _outputData, null));
        else
            await _onStep(MakeEvent("node_error", [], _errorMessage));
    }

    private NodeStepEvent MakeEvent(
        string eventType,
        Dictionary<string, object?> outputData,
        string? errorMessage)
        => new()
        {
            EventType         = eventType,
            NodeId            = _nodeDef.Id,
            NodeName          = _nodeDef.Name,
            NodeType          = _nodeDef.Type,
            InputData         = _inputData,
            OutputData        = outputData,
            DataInConfigured  = _dataInConfigured,
            DataOutConfigured = _dataOutConfigured,
            ErrorMessage      = errorMessage,
        };
}
