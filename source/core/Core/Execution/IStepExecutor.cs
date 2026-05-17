namespace Twf.Flow.Core.Execution;

/// <summary>
/// Main interface for executing workflow steps.
/// Implementations dispatch to specific step type executors.
/// </summary>
/// <remarks>
/// This abstraction enables the strategy pattern for step execution,
/// allowing new step types to be added without modifying existing code (Open/Closed Principle).
/// Internal interface - used within the framework for step execution.
/// </remarks>
internal interface IStepExecutor
{
    /// <summary>
    /// Executes a single workflow step.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="data">The current workflow data.</param>
    /// <param name="context">The workflow execution context.</param>
    /// <returns>The result of the step execution.</returns>
    Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step,
        WorkflowData data,
        WorkflowContext context);
}
