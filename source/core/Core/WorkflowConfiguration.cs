using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Twf.Flow.Core;

/// <summary>
/// Configuration options for workflow execution.
/// Separates configuration concerns from workflow structure and execution logic.
/// </summary>
public sealed class WorkflowConfiguration
{
    /// <summary>
    /// Logger for workflow execution events.
    /// </summary>
    public ILogger Logger { get; init; } = NullLogger.Instance;

    /// <summary>
    /// Callback invoked when workflow completes successfully.
    /// </summary>
    public Action<WorkflowResult>? OnComplete { get; init; }

    /// <summary>
    /// Callback invoked when workflow encounters an error.
    /// </summary>
    public Action<string, Exception?>? OnError { get; init; }

    /// <summary>
    /// Strategy for handling errors during workflow execution.
    /// </summary>
    public GlobalErrorStrategy ErrorStrategy { get; init; } = GlobalErrorStrategy.StopOnFirstFailure;

    /// <summary>
    /// Creates default configuration with a null logger.
    /// </summary>
    public static WorkflowConfiguration Default => new();

    /// <summary>
    /// Creates configuration with specified logger.
    /// </summary>
    public static WorkflowConfiguration WithLogger(ILogger logger) => new() { Logger = logger };

    /// <summary>
    /// Creates configuration with error callbacks.
    /// </summary>
    public static WorkflowConfiguration WithCallbacks(
      Action<WorkflowResult>? onComplete = null,
        Action<string, Exception?>? onError = null) => new()
        {
            OnComplete = onComplete,
            OnError = onError
        };
}

/// <summary>
/// Defines how a workflow handles errors.
/// </summary>
public enum GlobalErrorStrategy
{
    /// <summary>
    /// Stop execution immediately when a node fails (default behavior).
    /// </summary>
    StopOnFirstFailure = 0,

    /// <summary>
    /// Continue executing subsequent steps even if nodes fail.
    /// </summary>
    ContinueOnFailure = 1
}
