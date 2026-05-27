using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.Control;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.concepts;

/// <summary>
/// Concept 3: Parallel Execution
/// Demonstrates running multiple nodes concurrently and merging their results
/// </summary>
public static class ParallelExecution
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n📌 Concept 3: Parallel execution\n");

        using var logFactory = LoggerFactory.Create(b =>
           b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = logFactory.CreateLogger("ParallelExec");

        var parallelWorkflow = WorkflowBuilder.Create("ParallelDemo")
            .UseLogger(logger)
            .Parallel(
                new TransformNode("Task1", d => d.Clone().Set("result_1", "Alpha completed")),
                new TransformNode("Task2", d =>
                {
                    Thread.Sleep(50); // Simulate different durations
                    return d.Clone().Set("result_2", "Beta completed");
                }),
                new TransformNode("Task3", d => d.Clone().Set("result_3", "Gamma completed"))
               )
            .AddStep("MergeResults", (data, _) =>
            {
                var merged = $"{data.GetString("result_1")} | {data.GetString("result_2")} | {data.GetString("result_3")}";
                return Task.FromResult(data.Set("merged", merged));
            })
            .OnComplete(r => Console.WriteLine($"  ✅ Parallel results: {r.Data.GetString("merged")}"));

        await parallelWorkflow.RunAsync(new WorkflowData());
    }
}
