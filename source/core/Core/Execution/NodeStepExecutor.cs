using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Executes individual node steps with retry, timeout, and error handling.
/// </summary>
/// <remarks>
/// Handles:
/// - Conditional execution (RunCondition)
/// - Node execution tracking
/// - Retry with exponential backoff
/// - Timeout enforcement
/// - Error handling strategies (ContinueOnError)
/// </remarks>
internal sealed class NodeStepExecutor : ITypedStepExecutor
{
    /// <inheritdoc/>
    public StepType SupportedType => StepType.Node;

    /// <inheritdoc/>
    public async Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step,
        WorkflowData data,
        WorkflowContext context)
    {
        var node = step.Node ?? throw new InvalidOperationException("Node step has no node");
        var opts = step.Options;

        // Check run condition - skip if false
        if (!ShouldExecuteNode(step, data, context))
        {
            context.Logger.LogInformation("⏭  [{Node}] Skipped (condition not met)", node.Name);
            var skipped = NodeResult.Skipped(node.Name, data);
            return StepExecutionResult.Ok(data, new[] { skipped });
        }

        // Track node execution
        var record = context.Tracker.BeginNode(node.Name, node.Category);

        // Execute with retry and timeout
        var result = await ExecuteWithRetryAndTimeoutAsync(node, data, context, opts)
         .ConfigureAwait(false);

        context.Tracker.CompleteNode(record, result);

        // Handle result based on options
        return HandleNodeResult(result, data, opts, context);
    }

    /// <summary>
    /// Checks if the node should execute based on its run condition.
    /// </summary>
    private static bool ShouldExecuteNode(
        PipelineStep step,
     WorkflowData data,
        WorkflowContext context)
    {
        if (step.Options.RunCondition is null)
            return true;

        try
        {
            return step.Options.RunCondition(data);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(
     ex,
         "⚠️  [{Node}] Run condition threw exception, skipping node",
       step.Node?.Name ?? "Unknown");
            return false;
        }
    }

    /// <summary>
    /// Executes the node with retry logic and optional timeout.
    /// </summary>
    private static async Task<NodeResult> ExecuteWithRetryAndTimeoutAsync(
        INode node,
    WorkflowData data,
        WorkflowContext context,
   NodeOptions opts)
    {
        NodeResult? result = null;
        var attempts = 0;
        var maxAttempts = opts.MaxRetries + 1;

        while (attempts < maxAttempts)
        {
            attempts++;

            // Execute with timeout if configured
            if (opts.Timeout.HasValue)
            {
                result = await ExecuteWithTimeoutAsync(node, data, context, opts.Timeout.Value)
      .ConfigureAwait(false);
            }
            else
            {
                result = await node.ExecuteAsync(data, context)
              .ConfigureAwait(false);
            }

            // Success or no more retries
            if (result.IsSuccess || attempts >= maxAttempts)
                break;

            // Retry with exponential backoff
            var delay = CalculateRetryDelay(attempts, opts.RetryDelay);
            context.Logger.LogWarning(
                  "🔄 [{Node}] Attempt {Attempt}/{Max} failed. Retrying in {Delay}ms...",
         node.Name, attempts, maxAttempts, delay.TotalMilliseconds);

            await Task.Delay(delay, context.CancellationToken).ConfigureAwait(false);
        }

        return result ?? NodeResult.Failure(
        node.Name,
           data,
        "Node execution failed with no result",
                  null,
         TimeSpan.Zero,
                  DateTime.UtcNow);
    }

    /// <summary>
    /// Executes a node with a timeout.
    /// </summary>
    private static async Task<NodeResult> ExecuteWithTimeoutAsync(
        INode node,
        WorkflowData data,
 WorkflowContext context,
        TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(timeout);

        var timeoutContext = new WorkflowContext(
            context.WorkflowName,
           context.Logger,
      context.Tracker,
    cts.Token);

        // Copy state from original context
        foreach (var kvp in context.State.GetAll())
        {
            timeoutContext.State.Set(kvp.Key, kvp.Value);
        }

        try
        {
            return await node.ExecuteAsync(data, timeoutContext).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !context.CancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            return NodeResult.Failure(
                  node.Name,
                 data,
              $"Node execution timed out after {timeout.TotalSeconds}s",
             new TimeoutException($"Node '{node.Name}' exceeded timeout of {timeout}"),
           timeout,
              DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Calculates retry delay with exponential backoff.
    /// </summary>
    private static TimeSpan CalculateRetryDelay(int attempt, TimeSpan baseDelay)
    {
        var delayMs = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, 30000)); // Cap at 30 seconds
    }

    /// <summary>
    /// Handles the node execution result based on options.
    /// </summary>
    private static StepExecutionResult HandleNodeResult(
    NodeResult result,
    WorkflowData originalData,
 NodeOptions opts,
    WorkflowContext context)
    {
        if (result.IsSuccess)
        {
            return StepExecutionResult.Ok(result.Data, new[] { result });
        }

        // Handle failure
        if (opts.ContinueOnError)
        {
            context.Logger.LogWarning(
                  "⚠️  [{Node}] Failed but ContinueOnError=true. Using fallback data.",
                  result.NodeName);

            var fallbackData = opts.FallbackData ?? originalData;
            return StepExecutionResult.Ok(fallbackData, new[] { result });
        }

        return StepExecutionResult.Fail(result, originalData);
    }
}
