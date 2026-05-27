using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Control;

// ═══════════════════════════════════════════════════════════════════════════════
// TryCatchNode — Container node with embedded try/catch workflows
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Executes an embedded try workflow and, if it fails, executes an embedded catch workflow.
/// This behaves like a containerized try/catch block for workflow composition.
///
/// On try failure, catch workflow input is enriched with:
///   - "caught_error_message"
///   - "caught_failed_node"
///   - "caught_exception_type"
///
/// Writes:
///   - "try_catch_route" : "success" | "catch"
///   - "try_success"     : bool
///   - "try_error"       : bool
/// </summary>
public sealed class TryCatchNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Control";
    public override string Description => "Executes try workflow and catches failures with fallback workflow";

    private readonly WorkflowStructure _tryWorkflow;
    private readonly WorkflowStructure? _catchWorkflow;

    public TryCatchNode(
        string name,
        Action<WorkflowBuilder> tryBuilder,
        Action<WorkflowBuilder>? catchBuilder = null)
    {
        Name = name;

        var tryWorkflow = WorkflowBuilder.Create($"{name}/Try");
        tryBuilder(tryWorkflow);
        _tryWorkflow = tryWorkflow.Build();

        if (catchBuilder is not null)
        {
            var catchWorkflow = WorkflowBuilder.Create($"{name}/Catch");
            catchBuilder(catchWorkflow);
            _catchWorkflow = catchWorkflow.Build();
        }
    }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var executor = new WorkflowExecutor();
        var tryResult = await executor.ExecuteAsync(_tryWorkflow, input.Clone(), context);
        if (tryResult.IsSuccess)
        {
            nodeCtx.Log("Try workflow completed successfully");
            return tryResult.Data.Clone()
                .Set("try_catch_route", "success")
                .Set("try_success", true)
                .Set("try_error", false);
        }

        nodeCtx.Log($"Try workflow failed at '{tryResult.FailedNodeName}': {tryResult.ErrorMessage}");

        var catchInput = tryResult.Data.Clone()
            .Set("caught_error_message", tryResult.ErrorMessage ?? string.Empty)
            .Set("caught_failed_node", tryResult.FailedNodeName ?? string.Empty)
            .Set("caught_exception_type", tryResult.Exception?.GetType().Name ?? string.Empty);

        if (_catchWorkflow is null)
        {
            throw new InvalidOperationException(
                $"[{Name}] Try workflow failed and no catch workflow is configured. " +
                $"Failed node: {tryResult.FailedNodeName}, Error: {tryResult.ErrorMessage}");
        }

        var catchResult = await executor.ExecuteAsync(_catchWorkflow, catchInput, context);
        if (catchResult.IsSuccess)
        {
            nodeCtx.Log("Catch workflow handled the failure successfully");
            return catchResult.Data.Clone()
                .Set("try_catch_route", "catch")
                .Set("try_success", false)
                .Set("try_error", true);
        }

        throw new InvalidOperationException(
            $"[{Name}] Catch workflow failed while handling try failure. " +
            $"Failed node: {catchResult.FailedNodeName}, Error: {catchResult.ErrorMessage}");
    }

    public static TryCatchNode Create(
        string name,
        Action<WorkflowBuilder> tryBuilder,
        Action<WorkflowBuilder>? catchBuilder = null)
    {
        return new TryCatchNode(name, tryBuilder, catchBuilder);
    }
}