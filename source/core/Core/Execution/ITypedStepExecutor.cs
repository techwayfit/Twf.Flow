using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Interface for executors that handle specific step types.
/// Used by the strategy pattern to dispatch execution to the appropriate handler.
/// </summary>
/// <remarks>
/// Each implementation handles one <see cref="StepType"/> (Node, Branch, Parallel, Loop).
/// New step types can be added by implementing this interface without modifying existing code.
/// </remarks>
internal interface ITypedStepExecutor
{
    /// <summary>
    /// The step type this executor handles.
    /// </summary>
    StepType SupportedType { get; }

  /// <summary>
    /// Executes a step of the supported type.
    /// </summary>
    /// <param name="step">The step to execute (must be of <see cref="SupportedType"/>).</param>
    /// <param name="data">The current workflow data.</param>
    /// <param name="context">The workflow execution context.</param>
    /// <returns>The result of the step execution.</returns>
    Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step,
WorkflowData data,
        WorkflowContext context);
}
