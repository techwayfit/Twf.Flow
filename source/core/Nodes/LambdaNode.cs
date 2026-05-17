using Twf.Flow.Core;

namespace twf_ai_framework.Nodes;

/// <summary>Wraps a lambda as an INode — for quick inline steps.</summary>
internal sealed class LambdaNode : INode
{
    private readonly Func<WorkflowData, WorkflowContext, Task<WorkflowData>> _func;

    public string Name { get; }
    public string Category => "Lambda";
    public string Description => $"Inline step: {Name}";
    public string IdPrefix => "lambda";
    public IReadOnlyList<NodeData> DataIn  => [];
    public IReadOnlyList<NodeData> DataOut => [];

    public LambdaNode(string name, Func<WorkflowData, WorkflowContext, Task<WorkflowData>> func)
    {
        Name = name;
        _func = func;
    }

    public async Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context)
    {
        var start = DateTime.UtcNow;
        try
        {
            var output = await _func(data, context);
            return NodeResult.Success(Name, output, DateTime.UtcNow - start, start);
        }
        catch (Exception ex)
        {
            return NodeResult.Failure(Name, data, ex.Message, ex, DateTime.UtcNow - start, start);
        }
    }
}
