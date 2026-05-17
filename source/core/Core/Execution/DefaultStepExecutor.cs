using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Execution;

/// <summary>
/// Default implementation of <see cref="IStepExecutor"/> that dispatches to type-specific executors.
/// </summary>
/// <remarks>
/// Uses the Strategy Pattern to delegate execution to the appropriate executor based on step type.
/// New step types can be added by implementing <see cref="ITypedStepExecutor"/> and registering here.
/// </remarks>
internal sealed class DefaultStepExecutor : IStepExecutor
{
    private readonly Dictionary<StepType, ITypedStepExecutor> _executors;

    /// <summary>
    /// Initializes a new instance with all built-in step executors.
    /// </summary>
    public DefaultStepExecutor()
    {
        // Register all built-in executors
        var executors = new ITypedStepExecutor[]
        {
   new NodeStepExecutor(),
   new BranchStepExecutor(),
  new ParallelStepExecutor(),
   new LoopStepExecutor()
 };

        _executors = executors.ToDictionary(e => e.SupportedType);
    }

    /// <summary>
    /// Initializes a new instance with custom executors (for testing/extensibility).
    /// </summary>
    /// <param name="executors">The executors to register.</param>
    internal DefaultStepExecutor(IEnumerable<ITypedStepExecutor> executors)
    {
        _executors = executors.ToDictionary(e => e.SupportedType);
    }

    /// <inheritdoc/>
    public Task<StepExecutionResult> ExecuteAsync(
   PipelineStep step,
        WorkflowData data,
  WorkflowContext context)
    {
        if (!_executors.TryGetValue(step.Type, out var executor))
        {
            var errorMessage = $"Unknown step type: {step.Type}. " +
                   $"Available types: {string.Join(", ", _executors.Keys)}";

            context.Logger.LogError("❌ {Error}", errorMessage);

            throw new InvalidOperationException(errorMessage);
        }

        return executor.ExecuteAsync(step, data, context);
    }

    /// <summary>
    /// Gets all registered step types.
    /// </summary>
    internal IReadOnlyCollection<StepType> RegisteredTypes => _executors.Keys;

    /// <summary>
    /// Checks if a step type is registered.
    /// </summary>
    internal bool IsRegistered(StepType type) => _executors.ContainsKey(type);
}
