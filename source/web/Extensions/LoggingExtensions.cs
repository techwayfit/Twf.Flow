using Microsoft.Extensions.Logging;

namespace Twf.Flow.Web.Extensions;

/// <summary>
/// Extension methods for enhanced structured logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
 /// Begins a logging scope with workflow execution context.
/// </summary>
    public static IDisposable? BeginWorkflowScope(
        this ILogger logger,
    Guid workflowId,
        string workflowName,
        int nodeCount)
{
        return logger.BeginScope(new Dictionary<string, object>
        {
   ["WorkflowId"] = workflowId,
  ["WorkflowName"] = workflowName,
    ["NodeCount"] = nodeCount,
            ["Timestamp"] = DateTime.UtcNow
     });
    }

    /// <summary>
    /// Begins a logging scope with node execution context.
    /// </summary>
    public static IDisposable? BeginNodeScope(
   this ILogger logger,
        Guid nodeId,
   string nodeName,
        string nodeType)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["NodeId"] = nodeId,
   ["NodeName"] = nodeName,
            ["NodeType"] = nodeType,
       ["Timestamp"] = DateTime.UtcNow
     });
    }

    /// <summary>
    /// Begins a logging scope with repository operation context.
    /// </summary>
    public static IDisposable? BeginRepositoryScope(
        this ILogger logger,
        string operation,
     string entityType,
        Guid? entityId = null)
    {
  var scope = new Dictionary<string, object>
        {
 ["Operation"] = operation,
    ["EntityType"] = entityType,
            ["Timestamp"] = DateTime.UtcNow
        };

  if (entityId.HasValue)
        {
            scope["EntityId"] = entityId.Value;
   }

        return logger.BeginScope(scope);
    }

    /// <summary>
    /// Logs a workflow execution event with structured data.
    /// </summary>
    public static void LogWorkflowExecution(
   this ILogger logger,
        LogLevel logLevel,
        string eventType,
     Guid workflowId,
   string workflowName,
        string message,
        Exception? exception = null)
    {
      using (logger.BeginScope(new Dictionary<string, object>
    {
            ["EventType"] = eventType,
    ["WorkflowId"] = workflowId,
       ["WorkflowName"] = workflowName
        }))
        {
            logger.Log(logLevel, exception, message);
  }
    }

    /// <summary>
 /// Logs a node execution event with structured data.
    /// </summary>
    public static void LogNodeExecution(
   this ILogger logger,
    LogLevel logLevel,
        string eventType,
        Guid nodeId,
        string nodeName,
string nodeType,
string message,
        Exception? exception = null)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
     ["EventType"] = eventType,
    ["NodeId"] = nodeId,
 ["NodeName"] = nodeName,
    ["NodeType"] = nodeType
   }))
      {
logger.Log(logLevel, exception, message);
        }
    }

    /// <summary>
 /// Logs a performance metric.
    /// </summary>
    public static void LogPerformanceMetric(
   this ILogger logger,
     string metricName,
        double value,
        string? unit = null,
        Dictionary<string, object>? additionalProperties = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["MetricName"] = metricName,
          ["MetricValue"] = value,
      ["Timestamp"] = DateTime.UtcNow
        };

        if (unit != null)
        {
       properties["Unit"] = unit;
        }

   if (additionalProperties != null)
        {
          foreach (var (key, val) in additionalProperties)
       {
        properties[key] = val;
      }
        }

        using (logger.BeginScope(properties))
     {
  logger.LogInformation("Performance metric: {MetricName} = {MetricValue} {Unit}",
      metricName, value, unit ?? "");
        }
    }
}
