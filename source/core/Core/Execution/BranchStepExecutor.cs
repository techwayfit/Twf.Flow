using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Executes conditional branch steps.
/// </summary>
/// <remarks>
/// Evaluates a condition and executes either the true or false branch workflow.
/// Only one branch is executed per invocation.
/// </remarks>
internal sealed class BranchStepExecutor : ITypedStepExecutor
{
    /// <inheritdoc/>
    public StepType SupportedType => StepType.Branch;

    /// <inheritdoc/>
    public async Task<StepExecutionResult> ExecuteAsync(
   PipelineStep step,
        WorkflowData data,
 WorkflowContext context)
    {
        var condition = step.BranchCondition
     ?? throw new InvalidOperationException("Branch step has no condition");

        // Evaluate condition
        bool branchTaken;
        try
        {
            branchTaken = condition(data);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
   ex,
     "❌ Branch condition evaluation failed");

            return StepExecutionResult.Fail(
        NodeResult.Failure(
      "BranchCondition",
                data,
      $"Branch condition threw exception: {ex.Message}",
      ex,
         TimeSpan.Zero,
          DateTime.UtcNow),
                  data);
        }

        var pipeline = branchTaken ? step.TrueBranch : step.FalseBranch;

        context.Logger.LogInformation(
        "🔀 Branch condition: {Result}",
        branchTaken ? "TRUE" : "FALSE");

        // No pipeline for this branch
        if (pipeline is null)
        {
            context.Logger.LogInformation(
        "⏭  Branch {Branch} has no handler, skipping.",
        branchTaken ? "TRUE" : "FALSE");

            return StepExecutionResult.Ok(data, Array.Empty<NodeResult>());
        }

        // Execute the selected branch
        var branchExecutor = new WorkflowExecutor();
        var branchResult = await branchExecutor
            .ExecuteAsync(pipeline, data, context)
            .ConfigureAwait(false);

        if (branchResult.IsSuccess)
        {
            return StepExecutionResult.Ok(
        branchResult.Data,
     branchResult.NodeResults.ToList());
        }

        // Branch execution failed
        var lastResult = branchResult.NodeResults.LastOrDefault()
      ?? NodeResult.Failure(
      $"Branch_{branchTaken}",
  data,
     branchResult.ErrorMessage ?? "Branch execution failed",
        branchResult.Exception,
             TimeSpan.Zero,
      DateTime.UtcNow);

        return StepExecutionResult.Fail(lastResult, data);
    }
}
