namespace Twf.Flow.Core;

/// <summary>
/// Immutable workflow structure representing the architecture of a workflow.
/// Created by <see cref="WorkflowBuilder"/> and executed by <see cref="Execution.WorkflowExecutor"/>.
/// </summary>
/// <remarks>
/// This class follows the Single Responsibility Principle by only representing
/// the workflow structure without containing execution logic.
/// Named "WorkflowStructure" to avoid conflict with web project's WorkflowDefinition model.
/// </remarks>
public sealed class WorkflowStructure
{
    /// <summary>
    /// The name of the workflow.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The ordered sequence of steps in the workflow.
    /// </summary>
    internal IReadOnlyList<PipelineStep> Steps { get; }

    /// <summary>
    /// Configuration options for workflow execution.
    /// </summary>
    public WorkflowConfiguration Configuration { get; }

    /// <summary>
    /// Gets the number of steps in the workflow.
    /// </summary>
    public int StepCount => Steps.Count;

    /// <summary>
    /// Initializes a new workflow structure.
    /// </summary>
    /// <param name="name">The workflow name.</param>
    /// <param name="steps">The workflow steps.</param>
    /// <param name="configuration">Execution configuration.</param>
    /// <remarks>
    /// Constructor is internal to enforce creation through <see cref="WorkflowBuilder"/>.
    /// </remarks>
    internal WorkflowStructure(
        string name,
        IReadOnlyList<PipelineStep> steps,
        WorkflowConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name cannot be empty", nameof(name));

        Name = name;
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets a summary of the workflow structure.
    /// </summary>
    public WorkflowSummary GetSummary()
    {
        // Count steps by type using internal access
        var nodeCount = 0;
        var branchCount = 0;
        var loopCount = 0;
        var parallelCount = 0;

        foreach (var step in Steps)
        {
            switch (step.Type)
            {
                case twf_ai_framework.Core.Models.StepType.Node:
                    nodeCount++;
                    break;
                case twf_ai_framework.Core.Models.StepType.Branch:
                    branchCount++;
                    break;
                case twf_ai_framework.Core.Models.StepType.Loop:
                    loopCount++;
                    break;
                case twf_ai_framework.Core.Models.StepType.Parallel:
                    parallelCount++;
                    break;
            }
        }

        return new WorkflowSummary(
            Name,
            Steps.Count,
            nodeCount,
            branchCount,
            loopCount,
            parallelCount);
    }

    public override string ToString() => $"WorkflowStructure(Name='{Name}', Steps={Steps.Count})";
}

/// <summary>
/// Summary information about a workflow's structure.
/// </summary>
public sealed record WorkflowSummary(
    string Name,
    int TotalSteps,
    int NodeCount,
    int BranchCount,
    int LoopCount,
    int ParallelCount);
