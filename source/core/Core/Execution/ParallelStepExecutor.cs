using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Executes multiple nodes in parallel.
/// </summary>
/// <remarks>
/// Each node receives a clone of the current data.
/// Results are merged back into a single WorkflowData instance.
/// If any node fails, the entire parallel step fails (fail-fast behavior).
/// </remarks>
internal sealed class ParallelStepExecutor : ITypedStepExecutor
{
    /// <inheritdoc/>
    public StepType SupportedType => StepType.Parallel;

    /// <inheritdoc/>
    public async Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step,
  WorkflowData data,
    WorkflowContext context)
    {
        var nodes = step.ParallelNodes
              ?? throw new InvalidOperationException("Parallel step has no nodes");

        if (nodes.Length == 0)
        {
            context.Logger.LogWarning("⚠️  Parallel step has no nodes to execute");
            return StepExecutionResult.Ok(data, Array.Empty<NodeResult>());
        }

        context.Logger.LogInformation(
     "⚡ Running {Count} nodes in parallel",
            nodes.Length);

        var startTime = DateTime.UtcNow;

        // Execute all nodes in parallel
        var tasks = nodes
          .Select(node => ExecuteNodeAsync(node, data.Clone(), context))
              .ToArray();

        NodeResult[] results;
        try
        {
            results = await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
          ex,
          "❌ Parallel execution encountered an exception");

            // Return partial results with failure
            var partialResults = tasks
                      .Where(t => t.IsCompletedSuccessfully)
                   .Select(t => t.Result)
                       .ToArray();

            return StepExecutionResult.Fail(
                NodeResult.Failure(
                 "ParallelExecution",
                 data,
         $"Parallel execution failed: {ex.Message}",
         ex,
        DateTime.UtcNow - startTime,
                  startTime),
                 data);
        }

        // Merge results
        var merged = data.Clone();
        var allNodeResults = new List<NodeResult>();

        foreach (var result in results)
        {
            allNodeResults.Add(result);

            if (result.IsSuccess)
            {
                // Merge successful result data
                merged.Merge(result.Data);
            }
        }

        // Check for any failures
        var firstFailure = results.FirstOrDefault(r => r.IsFailure);
        if (firstFailure is not null)
        {
            context.Logger.LogError(
             "❌ Parallel execution failed at node: {Node}",
               firstFailure.NodeName);

            return StepExecutionResult.Fail(firstFailure, data);
        }

        var duration = DateTime.UtcNow - startTime;
        context.Logger.LogInformation(
             "✅ Parallel execution completed in {Duration}ms",
           duration.TotalMilliseconds);

        return StepExecutionResult.Ok(merged, allNodeResults);
    }

    /// <summary>
    /// Executes a single node as part of parallel execution.
    /// </summary>
    private static async Task<NodeResult> ExecuteNodeAsync(
    INode node,
  WorkflowData data,
        WorkflowContext context)
    {
        try
        {
            return await node.ExecuteAsync(data, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
              ex,
            "❌ Node {Node} failed in parallel execution",
         node.Name);

            return NodeResult.Failure(
            node.Name,
               data,
           $"Node failed in parallel execution: {ex.Message}",
             ex,
           TimeSpan.Zero,
           DateTime.UtcNow);
        }
    }
}
