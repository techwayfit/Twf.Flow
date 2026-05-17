using Microsoft.Extensions.Logging;
using Twf.Flow.Core;

namespace Twf.Flow.Web.Services.Execution;

/// <summary>
/// Executes nodes with retry logic, timeout support, and exponential backoff.
/// Handles execution failures gracefully based on configured node options.
/// </summary>
public class RetryableNodeExecutor : INodeExecutor
{
    private readonly ILogger<RetryableNodeExecutor> _logger;

    public RetryableNodeExecutor(ILogger<RetryableNodeExecutor> logger)
    {
  _logger = logger;
    }

    public async Task<WorkflowData> ExecuteAsync(
   INode node,
 WorkflowData data,
   WorkflowContext context,
NodeOptions options)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= options.MaxRetries; attempt++)
        {
     // Apply exponential backoff for retries
       if (attempt > 0)
    {
    var backoff = TimeSpan.FromMilliseconds(
      options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

   _logger.LogInformation(
  "Retrying '{NodeName}' — attempt {Attempt}/{MaxRetries} (backoff {BackoffMs}ms)",
       node.Name,
     attempt,
       options.MaxRetries,
      (int)backoff.TotalMilliseconds);

    await Task.Delay(backoff, context.CancellationToken);
    }

  try
            {
       NodeResult result;

  // Execute with timeout if specified
        if (options.Timeout.HasValue)
            {
          result = await ExecuteWithTimeoutAsync(
  node,
  data,
       context,
options.Timeout.Value);
       }
       else
   {
         result = await node.ExecuteAsync(data, context);
      }

      // Check if execution was successful
        if (result.IsSuccess)
   {
            if (attempt > 0)
 {
       _logger.LogInformation(
  "Node '{NodeName}' succeeded after {Attempt} retries",
  node.Name,
   attempt);
  }

        return result.Data;
      }

 // Node returned a failure result
        lastException = new InvalidOperationException(
   result.ErrorMessage ?? $"Node '{node.Name}' returned a failure result");

   _logger.LogWarning(
      "Node '{NodeName}' returned failure: {ErrorMessage}",
       node.Name,
         lastException.Message);
   }
            catch (OperationCanceledException)
    {
       // Don't retry on cancellation
   _logger.LogWarning(
               "Node '{NodeName}' execution was cancelled",
          node.Name);
throw;
   }
  catch (Exception ex)
    {
      lastException = ex;

    _logger.LogWarning(
   ex,
         "Node '{NodeName}' threw exception on attempt {Attempt}/{MaxRetries}",
         node.Name,
     attempt,
options.MaxRetries);
            }
    }

        // All retries exhausted
        _logger.LogError(
            lastException,
            "Node '{NodeName}' failed after {MaxRetries} retries",
      node.Name,
  options.MaxRetries);

        throw lastException!;
    }

    /// <summary>
    /// Executes a node with a timeout constraint.
    /// </summary>
 private async Task<NodeResult> ExecuteWithTimeoutAsync(
 INode node,
        WorkflowData data,
   WorkflowContext context,
        TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
   context.CancellationToken);

   cts.CancelAfter(timeout);

        try
        {
    // Execute with timeout cancellation token
     return await node.ExecuteAsync(data, context);
  }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
     _logger.LogError(
   "Node '{NodeName}' exceeded timeout of {TimeoutMs}ms",
        node.Name,
    (int)timeout.TotalMilliseconds);

     throw new TimeoutException(
    $"Node '{node.Name}' exceeded timeout of {timeout.TotalMilliseconds}ms");
      }
    }
}
