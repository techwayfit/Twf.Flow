using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core;

/// <summary>
/// Facade for constructing and executing workflows.
/// Delegates to <see cref="WorkflowBuilder"/> and <see cref="WorkflowExecutor"/> internally.
/// </summary>
/// <remarks>
/// This class maintains backward compatibility with existing code.
/// For new code, consider using <see cref="WorkflowBuilder"/> directly:
/// 
/// <code>
/// // Old way (still works):
/// var result = await Workflow.Create("MyBot")
///     .AddNode(new PromptBuilderNode(...))
///     .RunAsync(initialData);
/// 
/// // New way (recommended):
/// var structure = WorkflowBuilder.Create("MyBot")
///     .AddNode(new PromptBuilderNode(...))
///     .Build();
/// 
/// var executor = new WorkflowExecutor();
/// var result = await executor.ExecuteAsync(structure, initialData);
/// </code>
/// </remarks>
public sealed class Workflow
{
    private readonly WorkflowBuilder _builder;

    public string Name => _builder.Build().Name;

    private Workflow(string name)
    {
        _builder = WorkflowBuilder.Create(name);
    }

    // ─── Entry Point ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new workflow builder.
    /// </summary>
    public static Workflow Create(string workflowName) => new(workflowName);

    // ─── Configuration ────────────────────────────────────────────────────────

    /// <summary>
    /// Configures the logger for workflow execution.
    /// </summary>
    public Workflow UseLogger(ILogger logger)
    {
        _builder.UseLogger(logger);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked when the workflow completes successfully.
    /// </summary>
    public Workflow OnComplete(Action<WorkflowResult> handler)
    {
        _builder.OnComplete(handler);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked when the workflow encounters an error.
    /// </summary>
    public Workflow OnError(Action<string, Exception?> handler)
    {
        _builder.OnError(handler);
        return this;
    }

    /// <summary>
    /// Configures the workflow to continue executing even if nodes fail.
    /// </summary>
    public Workflow ContinueOnErrors()
    {
        _builder.ContinueOnErrors();
        return this;
    }

    // ─── Node Registration ────────────────────────────────────────────────────

    /// <summary>Add a node with default options.</summary>
    public Workflow AddNode(INode node)
    {
        _builder.AddNode(node);
        return this;
    }

    /// <summary>Add a node with custom options (retry, timeout, condition).</summary>
    public Workflow AddNode(INode node, NodeOptions options)
    {
        _builder.AddNode(node, options);
        return this;
    }

    /// <summary>Add an inline lambda node without creating a class.</summary>
    public Workflow AddStep(
        string name,
        Func<WorkflowData, WorkflowContext, Task<WorkflowData>> func,
        NodeOptions? options = null)
    {
        _builder.AddStep(name, func, options);
        return this;
    }

    // ─── Control Flow ─────────────────────────────────────────────────────────

    /// <summary>
    /// Conditional branching — like an IF node in n8n.
    /// Evaluates condition against the current WorkflowData.
    /// Only one branch executes.
    /// </summary>
    public Workflow Branch(
        Func<WorkflowData, bool> condition,
        Action<Workflow> trueBranch,
        Action<Workflow>? falseBranch = null)
    {
        _builder.Branch(
            condition,
            tb => trueBranch(new Workflow(tb)),
            falseBranch == null ? null : fb => falseBranch(new Workflow(fb)));
        return this;
    }

    /// <summary>
    /// Run multiple nodes in parallel. Each receives a clone of the current data.
    /// Results are merged back (later keys overwrite earlier ones).
    /// </summary>
    public Workflow Parallel(params INode[] nodes)
    {
        _builder.Parallel(nodes);
        return this;
    }

    /// <summary>
    /// Iterate over a list in WorkflowData and run a sub-workflow for each item.
    /// Results are collected into a list under 'outputKey'.
    /// </summary>
    public Workflow ForEach(
        string itemsKey,
        string outputKey,
        Action<Workflow> bodyBuilder)
    {
        _builder.ForEach(
            itemsKey,
            outputKey,
            bb => bodyBuilder(new Workflow(bb)));
        return this;
    }

    // ─── Execution ────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the workflow.
    /// </summary>
    /// <param name="initialData">Initial workflow data (optional).</param>
    /// <param name="context">Execution context (optional, will be created if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workflow execution result.</returns>
    public Task<WorkflowResult> RunAsync(
        WorkflowData? initialData = null,
        WorkflowContext? context = null,
        CancellationToken cancellationToken = default)
    {
        // If context is provided, we need to use the executor directly
        // to pass it through (builder's RunAsync doesn't accept context)
        if (context != null)
        {
            var structure = _builder.Build();
            var executor = new WorkflowExecutor();
            return executor.ExecuteAsync(structure, initialData, context, cancellationToken);
        }

        // Use builder's convenience method
        return _builder.RunAsync(initialData, cancellationToken);
    }

    // ─── Helper constructor for nested workflows ──────────────────────────────

    /// <summary>
    /// Internal constructor to wrap a WorkflowBuilder (for nested workflows).
    /// </summary>
    private Workflow(WorkflowBuilder builder)
    {
        _builder = builder;
    }
}
