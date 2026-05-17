using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.concepts;

/// <summary>
/// Concept 2: Node Chaining & Branching
/// Demonstrates sequential node execution, transformations, conditions, and branching logic
/// </summary>
public static class NodeChainingAndBranching
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n?? Concept 2: Node chaining & branching\n");

        using var logFactory = LoggerFactory.Create(b =>
     b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = logFactory.CreateLogger("NodeChaining");

      var workflow = Workflow.Create("ConceptDemo")
         .UseLogger(logger)

            // Validate
            .AddNode(new FilterNode("Validate")
       .RequireNonEmpty("text")
             .MaxLength("text", 100))

   // Transform
            .AddNode(new TransformNode("Uppercase",
          d => d.Clone().Set("text_upper", d.GetString("text")?.ToUpper())))

      // Condition
        .AddNode(new ConditionNode("CheckLength",
                ("is_long", d => (d.GetString("text")?.Length ?? 0) > 20)))

   // Branch based on condition
          .Branch(
                condition: d => d.Get<bool>("is_long"),
         trueBranch: b => b.AddNode(new TransformNode("LongTextHandler",
       d => d.Clone().Set("result", $"LONG: {d.GetString("text_upper")}"))),
           falseBranch: b => b.AddNode(new TransformNode("ShortTextHandler",
                    d => d.Clone().Set("result", $"SHORT: {d.GetString("text_upper")}")))
          )

            // Log result
          .AddNode(LogNode.Keys("Final", "result", "is_long"))

            .OnComplete(r =>
       {
        Console.WriteLine($"  ? Pipeline completed: result = '{r.Data.GetString("result")}'");
     Console.WriteLine(r.Summary());
       });

        await workflow.RunAsync(WorkflowData.From("text", "Hello FlowForge!"));
        await workflow.RunAsync(WorkflowData.From("text", "Manas!"));
        await workflow.RunAsync(WorkflowData.From("text", "This is a longer text that exceeds 20 chars"));
}
}
