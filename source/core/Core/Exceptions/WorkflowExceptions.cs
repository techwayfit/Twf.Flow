namespace Twf.Flow.Core.Exceptions;

/// <summary>
/// Base exception for all workflow-related errors.
/// Provides context about which workflow and node caused the error.
/// </summary>
public abstract class WorkflowException : Exception
{
    /// <summary>
    /// Name of the workflow that encountered the error.
    /// </summary>
    public string WorkflowName { get; }

    /// <summary>
    /// Name of the node that caused the error (if applicable).
    /// </summary>
    public string? NodeName { get; }

    /// <summary>
    /// Unique run ID for the workflow execution (if available).
    /// </summary>
    public string? RunId { get; }

    protected WorkflowException(
            string message,
            string workflowName,
            string? nodeName = null,
            string? runId = null,
            Exception? innerException = null)
   : base(message, innerException)
    {
        WorkflowName = workflowName;
        NodeName = nodeName;
        RunId = runId;
    }

    /// <summary>
    /// Returns a formatted error message with workflow context.
    /// </summary>
    public override string ToString()
    {
        var context = NodeName != null
    ? $"[Workflow: {WorkflowName}][Node: {NodeName}]"
   : $"[Workflow: {WorkflowName}]";

        if (RunId != null)
            context += $"[RunId: {RunId}]";

        return $"{context} {base.ToString()}";
    }
}

/// <summary>
/// Exception thrown when a node fails to execute.
/// Wraps the original exception and provides workflow context.
/// </summary>
public class NodeExecutionException : WorkflowException
{
    /// <summary>
    /// The node's category (e.g., "AI", "Data", "Control").
    /// </summary>
    public string? NodeCategory { get; }

    /// <summary>
    /// Duration of the failed execution attempt.
    /// </summary>
    public TimeSpan? ExecutionDuration { get; }

    public NodeExecutionException(
        string nodeName,
        string workflowName,
      string message,
        string? nodeCategory = null,
        string? runId = null,
        TimeSpan? executionDuration = null,
Exception? innerException = null)
        : base(message, workflowName, nodeName, runId, innerException)
    {
        NodeCategory = nodeCategory;
        ExecutionDuration = executionDuration;
    }

    /// <summary>
    /// Factory method for creating from a failed node result.
    /// </summary>
    public static NodeExecutionException FromNodeResult(
   NodeResult result,
        string workflowName,
        string? runId = null)
    {
        var message = result.ErrorMessage ?? "Node execution failed";
        return new NodeExecutionException(
            result.NodeName,
        workflowName,
     message,
   runId: runId,
  executionDuration: result.Duration,
   innerException: result.Exception);
    }
}

/// <summary>
/// Exception thrown when required data is missing from WorkflowData.
/// </summary>
public class WorkflowDataMissingKeyException : WorkflowException
{
    /// <summary>
    /// The key that was not found in WorkflowData.
    /// </summary>
    public string MissingKey { get; }

    /// <summary>
    /// The type that was expected for the key (if known).
    /// </summary>
    public Type? ExpectedType { get; }

    public WorkflowDataMissingKeyException(
        string key,
        string workflowName,
        string? nodeName = null,
        Type? expectedType = null,
        string? runId = null)
        : base(
        BuildMessage(key, nodeName, expectedType),
        workflowName,
        nodeName,
runId)
    {
        MissingKey = key;
        ExpectedType = expectedType;
    }

    private static string BuildMessage(string key, string? nodeName, Type? expectedType)
    {
        var msg = $"Required key '{key}' not found in WorkflowData";
        if (nodeName != null)
            msg += $" (required by node '{nodeName}')";
        if (expectedType != null)
            msg += $". Expected type: {expectedType.Name}";
        return msg;
    }
}

/// <summary>
/// Exception thrown when a node is configured incorrectly.
/// </summary>
public class NodeConfigurationException : WorkflowException
{
    /// <summary>
    /// The configuration parameter that is invalid.
    /// </summary>
    public string? ParameterName { get; }

    /// <summary>
    /// The invalid value that was provided.
    /// </summary>
    public object? InvalidValue { get; }

    public NodeConfigurationException(
     string nodeName,
  string workflowName,
        string message,
        string? parameterName = null,
        object? invalidValue = null,
  string? runId = null)
    : base(message, workflowName, nodeName, runId)
    {
        ParameterName = parameterName;
        InvalidValue = invalidValue;
    }

    /// <summary>
    /// Factory method for configuration validation failures.
    /// </summary>
    public static NodeConfigurationException InvalidParameter(
     string nodeName,
        string workflowName,
   string parameterName,
     object? invalidValue,
        string reason)
    {
        var message = $"Invalid configuration for parameter '{parameterName}': {reason}";
        if (invalidValue != null)
            message += $" (provided value: {invalidValue})";

        return new NodeConfigurationException(
       nodeName,
                   workflowName,
        message,
          parameterName,
        invalidValue);
    }
}

/// <summary>
/// Exception thrown when workflow data type conversion fails.
/// </summary>
public class WorkflowDataTypeException : WorkflowException
{
    /// <summary>
    /// The key whose value couldn't be converted.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The actual type of the value.
    /// </summary>
    public Type ActualType { get; }

    /// <summary>
    /// The type that was requested.
    /// </summary>
    public Type RequestedType { get; }

    public WorkflowDataTypeException(
        string key,
        Type actualType,
        Type requestedType,
     string workflowName,
        string? nodeName = null,
        string? runId = null)
 : base(
         $"Cannot convert key '{key}' from {actualType.Name} to {requestedType.Name}",
    workflowName,
         nodeName,
   runId)
    {
        Key = key;
        ActualType = actualType;
        RequestedType = requestedType;
    }
}

/// <summary>
/// Exception thrown when a workflow is cancelled by the user.
/// </summary>
public class WorkflowCancelledException : WorkflowException
{
    /// <summary>
    /// The node that was executing when cancellation was requested.
    /// </summary>
    public string? CurrentNodeName { get; }

    public WorkflowCancelledException(
        string workflowName,
        string? currentNodeName = null,
   string? runId = null)
        : base(
            "Workflow execution was cancelled",
     workflowName,
            currentNodeName,
            runId)
    {
        CurrentNodeName = currentNodeName;
    }
}

/// <summary>
/// Exception thrown when a workflow times out.
/// </summary>
public class WorkflowTimeoutException : WorkflowException
{
    /// <summary>
    /// The timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// The node that was executing when timeout occurred.
    /// </summary>
    public string? CurrentNodeName { get; }

    public WorkflowTimeoutException(
        string workflowName,
        TimeSpan timeout,
        string? currentNodeName = null,
        string? runId = null)
  : base(
            $"Workflow execution exceeded timeout of {timeout.TotalSeconds:F0}s",
            workflowName,
            currentNodeName,
     runId)
    {
        Timeout = timeout;
        CurrentNodeName = currentNodeName;
    }
}

/// <summary>
/// Exception thrown when a retry limit is exceeded.
/// </summary>
public class RetryLimitExceededException : WorkflowException
{
    /// <summary>
    /// The maximum number of retries that were attempted.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// The list of exceptions from each retry attempt.
    /// </summary>
    public IReadOnlyList<Exception> AttemptExceptions { get; }

    public RetryLimitExceededException(
          string nodeName,
            string workflowName,
            int maxRetries,
          IReadOnlyList<Exception> attemptExceptions,
            string? runId = null)
        : base(
           $"Node '{nodeName}' failed after {maxRetries} retry attempts",
                workflowName,
                nodeName,
        runId,
           attemptExceptions.LastOrDefault())
    {
        MaxRetries = maxRetries;
        AttemptExceptions = attemptExceptions;
    }
}
