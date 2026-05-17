using Twf.Flow.Core;
using Twf.Flow.Web.Models;

namespace Twf.Flow.Web.Services.GraphWalker;

/// <summary>
/// Service responsible for traversing a workflow graph and orchestrating node execution.
/// Handles routing, branching, loops, sub-workflows, and error handling.
/// </summary>
public interface IWorkflowGraphWalker
{
  /// <summary>
  /// Walks a workflow graph starting from the specified node.
    /// </summary>
    /// <param name="configuration">Configuration for the graph walk including start node, data, and routing.</param>
    /// <returns>The result of the graph walk including final data and status.</returns>
    Task<WalkResult> WalkAsync(WalkConfiguration configuration);
}

/// <summary>
/// Configuration for a workflow graph walk operation.
/// </summary>
public class WalkConfiguration
{
    public required Guid StartNodeId { get; init; }
    public required WorkflowData Data { get; init; }
  public required WorkflowContext Context { get; init; }
    public required Dictionary<Guid, NodeDefinition> NodeMap { get; init; }
    public required Dictionary<(Guid NodeId, string Port), Guid> Routing { get; init; }
    public required WorkflowDefinition WorkflowDefinition { get; init; }
    public required Func<NodeStepEvent, Task> OnStep { get; init; }
}

/// <summary>
/// Result of a workflow graph walk operation.
/// </summary>
public class WalkResult
{
    public required WorkflowData Data { get; init; }
  public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FailedNodeName { get; init; }

    public static WalkResult CreateSuccess(WorkflowData data) => new()
{
        Data = data,
  Success = true
    };

    public static WalkResult CreateFailure(
   WorkflowData data,
      string errorMessage,
 string? failedNodeName = null) => new()
    {
   Data = data,
 Success = false,
  ErrorMessage = errorMessage,
        FailedNodeName = failedNodeName
    };
}
