using Twf.Flow.Core;

namespace Twf.Flow.Web.Services.Execution;

/// <summary>
/// Service responsible for executing individual nodes with support for
/// retry logic, timeouts, and error handling.
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Executes a node with the specified options (retry, timeout, continue-on-error).
    /// </summary>
    /// <param name="node">The node to execute.</param>
    /// <param name="data">The workflow data to pass to the node.</param>
    /// <param name="context">The workflow execution context.</param>
 /// <param name="options">Execution options (retry, timeout, error handling).</param>
    /// <returns>The updated workflow data after node execution.</returns>
    /// <exception cref="Exception">Thrown if the node fails and ContinueOnError is false.</exception>
    Task<WorkflowData> ExecuteAsync(
        INode node,
WorkflowData data,
  WorkflowContext context,
     NodeOptions options);
}
