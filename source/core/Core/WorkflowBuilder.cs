using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Twf.Flow.Core;

/// <summary>
/// Fluent builder for constructing workflow structures.
/// Follows the Builder pattern to separate construction from representation.
/// </summary>
/// <remarks>
/// This class is responsible ONLY for building the workflow structure.
/// Execution is handled by <see cref="Execution.WorkflowExecutor"/>.
/// 
/// Usage:
/// <code>
/// var structure = WorkflowBuilder.Create("MyWorkflow")
///     .AddNode(new PromptBuilderNode(...))
///     .AddNode(new LlmNode(...))
///     .Build();
/// 
/// var executor = new WorkflowExecutor();
/// var result = await executor.ExecuteAsync(structure, data);
/// </code>
/// </remarks>
public sealed class WorkflowBuilder
{
    private readonly string _name;
    private readonly List<PipelineStep> _steps = new();
    private ILogger _logger = NullLogger.Instance;
    private Action<WorkflowResult>? _onComplete;
    private Action<string, Exception?>? _onError;
    private GlobalErrorStrategy _errorStrategy = GlobalErrorStrategy.StopOnFirstFailure;

    private WorkflowBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name cannot be empty", nameof(name));

        _name = name;
    }

    /// <summary>
    /// Gets the workflow name.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Creates a new workflow builder.
    /// </summary>
    /// <param name="workflowName">The name of the workflow.</param>
    public static WorkflowBuilder Create(string workflowName) => new(workflowName);

    /// <summary>
    /// Implicitly builds this builder into a <see cref="WorkflowStructure"/>.
    /// </summary>
    public static implicit operator WorkflowStructure(WorkflowBuilder builder) => builder.Build();

    // ─── Configuration Methods ────────────────────────────────────────────────

    /// <summary>
    /// Configures the logger for workflow execution.
    /// </summary>
    public WorkflowBuilder UseLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked when the workflow completes successfully.
    /// </summary>
    public WorkflowBuilder OnComplete(Action<WorkflowResult> handler)
    {
        _onComplete = handler;
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked when the workflow encounters an error.
    /// </summary>
    public WorkflowBuilder OnError(Action<string, Exception?> handler)
    {
        _onError = handler;
        return this;
    }

    /// <summary>
    /// Configures the workflow to continue executing even if nodes fail.
    /// </summary>
    public WorkflowBuilder ContinueOnErrors()
    {
        _errorStrategy = GlobalErrorStrategy.ContinueOnFailure;
        return this;
    }

    // ─── Node Registration ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a node to the workflow with default options.
    /// </summary>
    public WorkflowBuilder AddNode(INode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        return AddNode(node, NodeOptions.Default);
    }

    /// <summary>
    /// Adds a node to the workflow with custom options (retry, timeout, condition).
    /// </summary>
    public WorkflowBuilder AddNode(INode node, NodeOptions options)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (options == null) throw new ArgumentNullException(nameof(options));

        _steps.Add(new PipelineStep(twf_ai_framework.Core.Models.StepType.Node, node, options));
        return this;
    }

    /// <summary>
    /// Adds an inline lambda node without creating a class.
    /// </summary>
    public WorkflowBuilder AddStep(
   string name,
        Func<WorkflowData, WorkflowContext, Task<WorkflowData>> func,
        NodeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Step name cannot be empty", nameof(name));
        if (func == null) throw new ArgumentNullException(nameof(func));

        var node = new twf_ai_framework.Nodes.LambdaNode(name, func);
        _steps.Add(new PipelineStep(
         twf_ai_framework.Core.Models.StepType.Node,
       node,
 options ?? NodeOptions.Default));
        return this;
    }

    // ─── Control Flow ─────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a conditional branch to the workflow.
    /// Evaluates condition against the current WorkflowData.
    /// Only one branch executes.
    /// </summary>
    public WorkflowBuilder Branch(
          Func<WorkflowData, bool> condition,
          Action<WorkflowBuilder> trueBranch,
          Action<WorkflowBuilder>? falseBranch = null)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (trueBranch == null) throw new ArgumentNullException(nameof(trueBranch));

        var truePipeline = Create($"{_name}/Branch:True");
        trueBranch(truePipeline);

        WorkflowBuilder? falsePipeline = null;
        if (falseBranch != null)
        {
            falsePipeline = Create($"{_name}/Branch:False");
            falseBranch(falsePipeline);
        }

        _steps.Add(new PipelineStep(twf_ai_framework.Core.Models.StepType.Branch)
        {
            BranchCondition = condition,
            TrueBranch = truePipeline.Build(),
            FalseBranch = falsePipeline?.Build()
        });

        return this;
    }

    /// <summary>
    /// Runs multiple nodes in parallel.
    /// Each receives a clone of the current data.
    /// Results are merged back (later keys overwrite earlier ones).
    /// </summary>
    public WorkflowBuilder Parallel(params INode[] nodes)
    {
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        if (nodes.Length == 0) throw new ArgumentException("At least one node is required", nameof(nodes));

        _steps.Add(new PipelineStep(twf_ai_framework.Core.Models.StepType.Parallel)
        {
            ParallelNodes = nodes
        });

        return this;
    }

    /// <summary>
    /// Iterates over a list in WorkflowData and runs a sub-workflow for each item.
    /// Results are collected into a list under 'outputKey'.
    /// </summary>
    public WorkflowBuilder ForEach(
           string itemsKey,
           string outputKey,
           Action<WorkflowBuilder> bodyBuilder)
    {
        if (string.IsNullOrWhiteSpace(itemsKey))
            throw new ArgumentException("Items key cannot be empty", nameof(itemsKey));
        if (string.IsNullOrWhiteSpace(outputKey))
            throw new ArgumentException("Output key cannot be empty", nameof(outputKey));
        if (bodyBuilder == null) throw new ArgumentNullException(nameof(bodyBuilder));

        var bodyPipeline = Create($"{_name}/Loop");
        bodyBuilder(bodyPipeline);

        _steps.Add(new PipelineStep(twf_ai_framework.Core.Models.StepType.Loop)
        {
            LoopItemsKey = itemsKey,
            LoopOutputKey = outputKey,
            LoopBody = bodyPipeline.Build()
        });

        return this;
    }

    // ─── Build Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the immutable workflow structure.
    /// </summary>
    /// <returns>An immutable <see cref="WorkflowStructure"/> that can be executed.</returns>
    public WorkflowStructure Build()
    {
        var config = new WorkflowConfiguration
        {
            Logger = _logger,
            OnComplete = _onComplete,
            OnError = _onError,
            ErrorStrategy = _errorStrategy
        };

        return new WorkflowStructure(_name, _steps.AsReadOnly(), config);
    }

    /// <summary>
    /// Convenience method: Build and execute the workflow in one call.
    /// </summary>
    /// <param name="initialData">Initial workflow data.</param>
    /// <param name="context">Execution context (optional, will be created if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<WorkflowResult> RunAsync(
          WorkflowData? initialData = null,
          WorkflowContext? context = null,
          CancellationToken cancellationToken = default)
    {
        var structure = Build();
        var executor = new WorkflowExecutor();
        return await executor.ExecuteAsync(structure, initialData, context, cancellationToken);
    }
}
