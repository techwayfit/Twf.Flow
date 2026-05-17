using twf_ai_framework.Core.Models;
using Twf.Flow.Tracking;

namespace Twf.Flow.Core;

internal sealed class PipelineStep
{
    public StepType Type { get; }
    public INode? Node { get; }
    public NodeOptions Options { get; }

    // Branch
    public Func<WorkflowData, bool>? BranchCondition { get; init; }
    public Workflow? TrueBranch { get; init; }
    public Workflow? FalseBranch { get; init; }

    // Parallel
    public INode[]? ParallelNodes { get; init; }

    // Loop
    public string? LoopItemsKey { get; init; }
    public string? LoopOutputKey { get; init; }
    public Workflow? LoopBody { get; init; }

    public PipelineStep(StepType type, INode? node = null, NodeOptions? options = null)
    {
        Type = type;
        Node = node;
        Options = options ?? NodeOptions.Default;
    }
}
