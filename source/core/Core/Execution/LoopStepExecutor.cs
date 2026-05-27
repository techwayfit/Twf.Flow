using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Executes loop (iteration) steps.
/// </summary>
/// <remarks>
/// Iterates over a collection in WorkflowData and executes a sub-workflow for each item.
/// Results are collected into a list and stored back in WorkflowData.
/// Special loop variables are injected: __loop_item__, __loop_index__, __loop_total__.
/// </remarks>
internal sealed class LoopStepExecutor : ITypedStepExecutor
{
    /// <inheritdoc/>
    public StepType SupportedType => StepType.Loop;

    /// <inheritdoc/>
    public async Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step,
        WorkflowData data,
        WorkflowContext context)
    {
        var itemsKey = step.LoopItemsKey
            ?? throw new InvalidOperationException("Loop step has no items key");
        var outputKey = step.LoopOutputKey
             ?? throw new InvalidOperationException("Loop step has no output key");
        var loopBody = step.LoopBody
                 ?? throw new InvalidOperationException("Loop step has no body");

        // Get items to iterate over
        var items = GetLoopItems(data, itemsKey, context);

        if (items.Count == 0)
        {
            context.Logger.LogInformation(
      "⏭  Loop has no items in '{Key}', skipping",
                 itemsKey);

            return StepExecutionResult.Ok(
                  data.Clone().Set(outputKey, new List<WorkflowData>()),
             Array.Empty<NodeResult>());
        }

        context.Logger.LogInformation(
             "🔁 Loop over {Count} items in '{Key}'",
       items.Count,
                itemsKey);

        var outputs = new List<WorkflowData>();
        var allResults = new List<NodeResult>();
        var startTime = DateTime.UtcNow;

        for (var i = 0; i < items.Count; i++)
        {
            // Inject loop variables
            var itemData = data.Clone()
         .Set("__loop_item__", items[i])
      .Set("__loop_index__", i)
       .Set("__loop_total__", items.Count);

            context.Logger.LogDebug(
         "  Iteration {Index}/{Total}",
                i + 1,
       items.Count);

            // Execute loop body
                        var loopExecutor = new WorkflowExecutor();
                        var loopResult = await loopExecutor
                                .ExecuteAsync(loopBody, itemData, context)
                                .ConfigureAwait(false);

            allResults.AddRange(loopResult.NodeResults);

            if (loopResult.IsFailure)
            {
                context.Logger.LogError(
                 "❌ Loop iteration {Index} failed: {Error}",
                     i,
            loopResult.ErrorMessage);

                return StepExecutionResult.Fail(
                      loopResult.NodeResults.LastOrDefault()
                      ?? NodeResult.Failure(
                          $"LoopIteration_{i}",
                         data,
                              loopResult.ErrorMessage ?? "Loop iteration failed",
                      loopResult.Exception,
                     DateTime.UtcNow - startTime,
                  startTime),
                       data);
            }

            outputs.Add(loopResult.Data);
        }

        var duration = DateTime.UtcNow - startTime;
        context.Logger.LogInformation(
                 "✅ Loop completed {Count} iterations in {Duration}ms",
        items.Count,
      duration.TotalMilliseconds);

        // Store results
        var resultData = data.Clone().Set(outputKey, outputs);
        return StepExecutionResult.Ok(resultData, allResults);
    }

    /// <summary>
    /// Retrieves the collection to iterate over from WorkflowData.
    /// </summary>
    private static List<object> GetLoopItems(
        WorkflowData data,
string itemsKey,
        WorkflowContext context)
    {
        // Try to get as enumerable
        var items = data.Get<IEnumerable<object>>(itemsKey);

        if (items is not null)
        {
            return items.ToList();
        }

        // Try as generic object and convert
        var obj = data.Get<object>(itemsKey);

        if (obj is IEnumerable<object> enumerable)
        {
            return enumerable.ToList();
        }

        // Try to enumerate any IEnumerable
        if (obj is System.Collections.IEnumerable nonGenericEnumerable)
        {
            var list = new List<object>();
            foreach (var item in nonGenericEnumerable)
            {
                list.Add(item);
            }
            return list;
        }

        context.Logger.LogWarning(
      "⚠️  Loop items key '{Key}' not found or not enumerable",
         itemsKey);

        return new List<object>();
    }
}
