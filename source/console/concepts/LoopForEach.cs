using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.Control;

namespace twf_ai_framework.console.concepts;

/// <summary>
/// Concept 4: Loop (ForEach)
/// Demonstrates iterating over collections and processing each item through a workflow
/// </summary>
public static class LoopForEach
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n?? Concept 4: Loop (ForEach)\n");

        using var logFactory = LoggerFactory.Create(b =>
           b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = logFactory.CreateLogger("LoopDemo");

        var items = new List<string> { "apple", "banana", "cherry" };
        var loopWorkflow = Workflow.Create("LoopDemo")
              .UseLogger(logger)
               .ForEach(
                    itemsKey: "fruits",
                    outputKey: "processed_fruits",
                    bodyBuilder: loop => loop
                    .AddStep("ProcessFruit", (data, _) =>
                       {
                           var fruit = data.GetString("__loop_item__")!;
                           return Task.FromResult(data.Set("processed", fruit.ToUpper() + "!"));
                       })
                    )
               .AddNode(new LogNode("LogProcessedFruit", ["__loop_item__"],LogLevel.Information))
               .OnComplete(r =>
                  {
                      var results = r.Data.Get<List<WorkflowData>>("processed_fruits");
                      var output = string.Join(", ", results?.Select(d => d.GetString("processed")) ?? Array.Empty<string>());
                      Console.WriteLine($"  ? Loop results: {output}");
                  });

        await loopWorkflow.RunAsync(WorkflowData.From("fruits", (object)items));
    }
}
