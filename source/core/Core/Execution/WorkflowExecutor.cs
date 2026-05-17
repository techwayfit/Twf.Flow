using Microsoft.Extensions.Logging;
using Twf.Flow.Core.Execution;

namespace Twf.Flow.Core;

/// <summary>
/// Executes workflow structures built by <see cref="WorkflowBuilder"/>.
/// Follows the Single Responsibility Principle - ONLY handles execution orchestration.
/// </summary>
/// <remarks>
/// This class orchestrates workflow execution by:
/// 1. Creating execution context
/// 2. Iterating through steps
/// 3. Delegating step execution to <see cref="IStepExecutor"/>
/// 4. Handling workflow-level error strategies
/// 5. Generating execution reports
/// 
/// Usage:
/// <code>
/// var structure = WorkflowBuilder.Create("MyWorkflow")
///     .AddNode(new TestNode())
///     .Build();
/// 
/// var executor = new WorkflowExecutor();
/// var result = await executor.ExecuteAsync(structure, initialData);
/// </code>
/// </remarks>
public sealed class WorkflowExecutor
{
    private readonly IStepExecutor _stepExecutor;

    /// <summary>
    /// Initializes a new workflow executor with default step executor.
    /// </summary>
    public WorkflowExecutor()
        : this(new DefaultStepExecutor())
    {
    }

    /// <summary>
    /// Initializes a new workflow executor with custom step executor (for testing/extensibility).
    /// </summary>
    /// <param name="stepExecutor">The step executor to use.</param>
    internal WorkflowExecutor(IStepExecutor stepExecutor)
    {
        _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
    }

    /// <summary>
    /// Executes a workflow structure.
    /// </summary>
    /// <param name="structure">The workflow structure to execute.</param>
    /// <param name="initialData">Initial workflow data (optional).</param>
    /// <param name="context">Execution context (optional, will be created if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow execution result.</returns>
    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowStructure structure,
     WorkflowData? initialData = null,
        WorkflowContext? context = null,
     CancellationToken cancellationToken = default)
    {
        if (structure == null) throw new ArgumentNullException(nameof(structure));

        var config = structure.Configuration;
        var ctx = context ?? new WorkflowContext(
    structure.Name,
      config.Logger,
  cancellationToken: cancellationToken);

        var startedAt = DateTime.UtcNow;
        var current = initialData?.Clone() ?? new WorkflowData();
        var allResults = new List<NodeResult>();

        ctx.Logger.LogInformation(
         "🚀 Starting workflow '{Workflow}' [RunId: {RunId}]",
          structure.Name,
  ctx.RunId);

        try
        {
            // Execute each step in sequence
            foreach (var step in structure.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepResult = await _stepExecutor
.ExecuteAsync(step, current, ctx)
        .ConfigureAwait(false);

                allResults.AddRange(stepResult.Results);

                if (stepResult.IsSuccess)
                {
                    current = stepResult.Data;
                }
                else if (config.ErrorStrategy == GlobalErrorStrategy.StopOnFirstFailure)
                {
                    // Workflow failed - stop execution
                    return CreateFailureResult(
               structure,
                  ctx,
                     current,
             allResults,
                stepResult,
                   startedAt,
                     config);
                }
                // If ContinueOnFailure, keep going
            }

            // Workflow completed successfully
            return CreateSuccessResult(
          structure,
      ctx,
    current,
   allResults,
     startedAt,
          config);
        }
        catch (OperationCanceledException)
        {
            ctx.Logger.LogWarning(
       "⚠️  Workflow '{Workflow}' was cancelled",
       structure.Name);

            return CreateCancelledResult(
               structure,
         ctx,
              current,
                  allResults,
                startedAt,
         config);
        }
        catch (Exception ex)
        {
            ctx.Logger.LogError(
        ex,
                      "❌ Workflow '{Workflow}' encountered an unexpected error",
              structure.Name);

            return CreateExceptionResult(
                 structure,
              ctx,
                   current,
                   allResults,
                        ex,
              startedAt,
         config);
        }
    }

    // ─── Result Creation Methods ──────────────────────────────────────────────

    private static WorkflowResult CreateSuccessResult(
        WorkflowStructure structure,
    WorkflowContext ctx,
        WorkflowData data,
      List<NodeResult> results,
  DateTime startedAt,
        WorkflowConfiguration config)
    {
        var report = ctx.Tracker.GenerateReport(structure.Name, ctx.RunId);
        var result = WorkflowResult.Success(
       structure.Name,
     ctx.RunId,
            data,
        results,
            startedAt,
        report);

        ctx.Logger.LogInformation(
                  "🏁 Workflow '{Workflow}' completed in {Duration}ms ✅",
                  structure.Name,
                  result.TotalDuration.TotalMilliseconds);

        config.OnComplete?.Invoke(result);
        return result;
    }

    private static WorkflowResult CreateFailureResult(
      WorkflowStructure structure,
        WorkflowContext ctx,
        WorkflowData data,
        List<NodeResult> results,
    StepExecutionResult stepResult,
        DateTime startedAt,
 WorkflowConfiguration config)
    {
        var report = ctx.Tracker.GenerateReport(structure.Name, ctx.RunId);
        var result = WorkflowResult.Failure(
            structure.Name,
        ctx.RunId,
                    data,
                    results,
           stepResult.FailedNodeName ?? "Unknown",
                    stepResult.ErrorMessage ?? "Unknown error",
               stepResult.Exception,
          startedAt,
            report);

        ctx.Logger.LogError(
      "💥 Workflow '{Workflow}' FAILED at [{Node}]: {Error}",
        structure.Name,
  result.FailedNodeName,
            result.ErrorMessage);

        config.OnError?.Invoke(result.ErrorMessage!, result.Exception);
        return result;
    }

    private static WorkflowResult CreateCancelledResult(
    WorkflowStructure structure,
   WorkflowContext ctx,
        WorkflowData data,
   List<NodeResult> results,
        DateTime startedAt,
        WorkflowConfiguration config)
    {
        var report = ctx.Tracker.GenerateReport(structure.Name, ctx.RunId);
        var result = WorkflowResult.Failure(
         structure.Name,
            ctx.RunId,
    data,
      results,
            "Cancellation",
      "Workflow execution was cancelled",
         new OperationCanceledException("Workflow was cancelled"),
      startedAt,
 report);

        config.OnError?.Invoke(result.ErrorMessage!, result.Exception);
        return result;
    }

    private static WorkflowResult CreateExceptionResult(
        WorkflowStructure structure,
  WorkflowContext ctx,
 WorkflowData data,
        List<NodeResult> results,
        Exception ex,
   DateTime startedAt,
        WorkflowConfiguration config)
    {
        var report = ctx.Tracker.GenerateReport(structure.Name, ctx.RunId);
        var result = WorkflowResult.Failure(
            structure.Name,
            ctx.RunId,
            data,
        results,
 "UnhandledException",
     ex.Message,
            ex,
            startedAt,
            report);

        config.OnError?.Invoke(result.ErrorMessage!, result.Exception);
        return result;
    }
}
