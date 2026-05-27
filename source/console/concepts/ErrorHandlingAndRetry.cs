using Microsoft.Extensions.Logging;
using Twf.Flow.Core;

namespace twf_ai_framework.console.concepts;

/// <summary>
/// Concept 5: Error Handling & Retry
/// Demonstrates retry mechanisms and error recovery strategies
/// </summary>
public static class ErrorHandlingAndRetry
{
    private static int _attempts = 0;

    public static async Task RunAsync()
  {
     Console.WriteLine("\n?? Concept 5: Error handling & retry\n");

        using var logFactory = LoggerFactory.Create(b =>
   b.AddConsole().SetMinimumLevel(LogLevel.Debug));
    var logger = logFactory.CreateLogger("ErrorHandling");

        _attempts = 0; // Reset counter for demo

        var errorWorkflow = WorkflowBuilder.Create("ErrorDemo")
       .UseLogger(logger)
            .AddStep("FlakyOperation", (data, _) =>
{
      _attempts++;
        if (_attempts < 3)
          throw new Exception($"Simulated failure on attempt {_attempts}");
                return Task.FromResult(data.Set("result", "Succeeded after retries!"));
          }, NodeOptions.WithRetry(3, TimeSpan.FromMilliseconds(100)))
    .OnComplete(r => Console.WriteLine($"  ? Error handling: {r.Data.GetString("result")}"))
            .OnError((msg, ex) => Console.WriteLine($"  ? Final error: {msg}"));

     await errorWorkflow.RunAsync(new WorkflowData());
    }
}
