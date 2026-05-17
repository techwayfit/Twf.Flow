using Twf.Flow.Core;

namespace twf_ai_framework.console.concepts;

/// <summary>
/// Concept 1: WorkflowData Fluent API
/// Demonstrates the fluent builder pattern for creating and manipulating workflow data
/// </summary>
public static class WorkflowDataFluentApi
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n?? Concept 1: WorkflowData fluent API\n");
        
        var data = WorkflowData.From("user_message", "Hello from FlowForge!")
.Set("user_id", 42)
            .Set("language", "en")
        .Set("tags", new List<string> { "demo", "test" });

        Console.WriteLine($"  data.GetString('user_message') = '{data.GetString("user_message")}'");
  Console.WriteLine($"  data.Get<int>('user_id') = {data.Get<int>("user_id")}");
 Console.WriteLine($"  data.Has('language')           = {data.Has("language")}");
        Console.WriteLine($"  data.Keys   = [{string.Join(", ", data.Keys)}]");
 Console.WriteLine($"  data.ToJson()            = {data.ToJson()}");

    await Task.CompletedTask;
    }
}
