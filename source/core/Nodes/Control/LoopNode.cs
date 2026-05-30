using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Control;

// ═══════════════════════════════════════════════════════════════════════════════
// LoopNode — ForEach iteration over a collection in WorkflowData
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Iterates over each item in a WorkflowData collection, runs an embedded body
/// workflow per item, and collects per-item results into an output key.
///
/// Reads from WorkflowData:
///   - <see cref="_itemsKey"/> : IEnumerable of items to loop over (any element type)
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>   : List&lt;WorkflowData&gt; — one entry per item
///   - "loop_iteration_count"     : total number of items processed
///
/// Body workflow receives per-item WorkflowData that includes all current keys
/// plus <see cref="_loopItemKey"/> set to the current item.
///
/// Usage (code-first):
/// <code>
///   new LoopNode("ProcessFruits",
///       itemsKey:     "fruits",
///       outputKey:    "processed_fruits",
///       bodyBuilder:  loop => loop
///           .AddNode(new TransformNode("Upper",
///               d => d.Clone().Set("result", d.GetString("__item__")?.ToUpper()))))
/// </code>
/// </summary>
public sealed class LoopNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Control";
    public override string Description =>
        $"Iterates over '{_itemsKey}', writing each result to '{_outputKey}'";

    /// <inheritdoc/>

    // WorkflowData keys — defaults for configurable keys and hardcoded internal keys
    public const string DefaultItemsKey    = "items";
    public const string DefaultOutputKey   = "results";
    public const string DefaultLoopItemKey = "__item__";
    public const string LoopIndexKey       = "__loop_index__";
    public const string OutputIterationCount = "loop_iteration_count";

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <remarks>
    /// Control ports (not data keys):
    ///   "body"   — connects to the first node of the per-item body chain (orange handle in UI).
    ///   "output" — connects to the next step after the loop completes (grey handle in UI).
    /// </remarks>

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>

    private readonly string _itemsKey;
    private readonly string _outputKey;
    private readonly string _loopItemKey;
    private readonly int _maxIterations;
    private readonly WorkflowStructure? _body;

    /// <param name="name">Node name shown in logs.</param>
    /// <param name="itemsKey">WorkflowData key that holds the collection to iterate.</param>
    /// <param name="outputKey">WorkflowData key where per-item results are written.</param>
    /// <param name="loopItemKey">Key injected into each iteration's WorkflowData for the current item.</param>
    /// <param name="maxIterations">Safety cap (0 = unlimited).</param>
    /// <param name="bodyBuilder">Fluent builder for the per-item sub-workflow.</param>
    public LoopNode(
        string name,
        string itemsKey      = DefaultItemsKey,
        string outputKey     = DefaultOutputKey,
        string loopItemKey   = DefaultLoopItemKey,
        int    maxIterations = 0,
        Action<WorkflowBuilder>? bodyBuilder = null)
    {
        Name           = name;
        _itemsKey      = itemsKey;
        _outputKey     = outputKey;
        _loopItemKey   = loopItemKey;
        _maxIterations = maxIterations;

        if (bodyBuilder is not null)
        {
            var body = WorkflowBuilder.Create($"{name}/Body");
            bodyBuilder(body);
            _body = body.Build();
        }
    }

    /// <summary>Dictionary constructor for dynamic instantiation (body sub-workflow is handled by the runner).</summary>
    public LoopNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Loop",
            NodeParameters.GetString(parameters, "itemsKey")    ?? DefaultItemsKey,
            NodeParameters.GetString(parameters, "outputKey")   ?? DefaultOutputKey,
            NodeParameters.GetString(parameters, "loopItemKey") ?? DefaultLoopItemKey,
            NodeParameters.GetInt(parameters, "maxIterations"))
    { }

    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var rawItems = input.Get<IEnumerable<object>>(_itemsKey)
            ?? throw new InvalidOperationException(
                $"LoopNode '{Name}': key '{_itemsKey}' not found or is not a collection.");

        var items = rawItems.ToList();
        if (_maxIterations > 0 && items.Count > _maxIterations)
        {
            nodeCtx.Log($"⚠️  Capping iteration at {_maxIterations} (total={items.Count})");
            items = items.Take(_maxIterations).ToList();
        }

        nodeCtx.Log($"Iterating over {items.Count} item(s) in '{_itemsKey}'");

        var outputs = new List<WorkflowData>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var itemData = input.Clone()
                .Set(_loopItemKey, items[i])
                .Set(LoopIndexKey, i);

            if (_body is not null)
            {
                var executor = new WorkflowExecutor();
                var result = await executor.ExecuteAsync(_body, itemData, context);
                if (!result.IsSuccess)
                {
                    nodeCtx.Log($"  ✘ Iteration {i} failed: {result.ErrorMessage}");
                    throw new InvalidOperationException(
                        $"LoopNode '{Name}': iteration {i} failed — {result.ErrorMessage}");
                }
                outputs.Add(result.Data);
            }
            else
            {
                // No body configured — just collect the item data (runner injects body separately)
                outputs.Add(itemData);
            }
        }

        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Control.IterationCount, items.Count);
        nodeCtx.Log($"Loop complete: {items.Count} iteration(s)");

        return input.Clone()
            .Set(_outputKey,          outputs)
            .Set(OutputIterationCount, items.Count);
    }
}
