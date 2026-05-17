using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.Data;

namespace twf_ai_framework.console.node_examples;

/// <summary>
/// Examples for all Data-category nodes:
/// - ChunkTextNode
/// - DataMapperNode
/// - FilterNode
/// - TransformNode
/// - SetVariableNode
/// - MemoryNode
/// </summary>
public static class DataNodeExamples
{
    public static async Task RunAllExamples()
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????????");
    Console.WriteLine("?      DATA NODE EXAMPLES    ?");
      Console.WriteLine("??????????????????????????????????????????????????????????????\n");

        await ChunkTextNodeExample();
        await DataMapperNodeExample();
    await FilterNodeExample();
 await TransformNodeExample();
        await SetVariableNodeExample();
   await MemoryNodeExample();
    }

    /// <summary>
    /// ChunkTextNode: Split large text into overlapping chunks for RAG
    /// Use Case: Document chunking for embedding/vector search
    /// </summary>
    private static async Task ChunkTextNodeExample()
    {
        Console.WriteLine("\n??? 1. ChunkTextNode Example ?????????????????????????????????");
        Console.WriteLine("Use Case: Split documentation into chunks for RAG pipeline\n");

     using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ChunkExample");

        var workflow = Workflow.Create("DocumentChunker")
      .UseLogger(logger)
            .AddNode(new ChunkTextNode(new ChunkConfig
          {
                ChunkSize = Twf.Flow.Core.ValueObjects.ChunkSize.FromValue(150),
      Overlap = Twf.Flow.Core.ValueObjects.ChunkOverlap.FromValue(20),
         Strategy = ChunkStrategy.Character
            }))
            .OnComplete(result =>
        {
  var chunks = result.Data.Get<List<TextChunk>>("chunks") ?? new();
       Console.WriteLine($"? Created {chunks.Count} chunks:\n");
           for (int i = 0; i < Math.Min(3, chunks.Count); i++)
   {
            Console.WriteLine($"  Chunk {i + 1}:");
 Console.WriteLine($"    Text: {chunks[i].Text[..Math.Min(100, chunks[i].Text.Length)]}...");
                Console.WriteLine($"    Length: {chunks[i].Text.Length} chars");
     Console.WriteLine($"    Range: [{chunks[i].StartPos}, {chunks[i].EndPos})");
      }
          if (chunks.Count > 3)
         Console.WriteLine($"  ... and {chunks.Count - 3} more chunks");
   });

        var input = new WorkflowData()
            .Set("text",
   "Twf.Flow is a powerful workflow engine for building AI-powered applications. " +
      "It provides a flexible, composable architecture that allows developers to chain together " +
     "AI models, data transformations, and control flow logic into sophisticated pipelines. " +
"The framework supports parallel execution, error handling, conditional branching, and " +
   "loops, making it ideal for complex AI workflows. It's designed to work with any " +
      "OpenAI-compatible API, including OpenAI, Anthropic, Ollama, and Azure OpenAI.")
.Set("source", "Twf.Flow Documentation");

        await workflow.RunAsync(input);

        // Word-based chunking example
        Console.WriteLine("\n  Word-Based Chunking Example:");
     var wordWorkflow = Workflow.Create("WordChunker")
       .UseLogger(logger)
      .AddNode(new ChunkTextNode(new ChunkConfig
        {
         ChunkSize = Twf.Flow.Core.ValueObjects.ChunkSize.FromValue(30),
        Overlap = Twf.Flow.Core.ValueObjects.ChunkOverlap.FromValue(5),
    Strategy = ChunkStrategy.Word
   }))
            .OnComplete(result =>
{
     var chunks = result.Data.Get<List<TextChunk>>("chunks") ?? new();
       Console.WriteLine($"? Created {chunks.Count} word-based chunks");
            });

        await wordWorkflow.RunAsync(input);
    }

    /// <summary>
    /// DataMapperNode: Map source keys to target keys with dot-path support
    /// Use Case: Transform API responses, rename fields, extract nested data
    /// </summary>
    private static async Task DataMapperNodeExample()
    {
        Console.WriteLine("\n??? 2. DataMapperNode Example ????????????????????????????????");
        Console.WriteLine("Use Case: Transform API response structure\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("MapperExample");

        var workflow = Workflow.Create("APITransformer")
            .UseLogger(logger)
 .AddNode(new DataMapperNode(
   name: "ExtractUserData",
         mappings: new Dictionary<string, string>
              {
            ["user_id"] = "response.data.user.id",
 ["user_name"] = "response.data.user.name",
    ["email"] = "response.data.user.contact.email",
           ["subscription_tier"] = "response.data.subscription.plan.tier",
        ["created_date"] = "response.data.user.createdAt"
     }
       ))
         .OnComplete(result =>
            {
 Console.WriteLine("? Extracted Fields:");
       Console.WriteLine($"  User ID: {result.Data.Get<string>("user_id")}");
             Console.WriteLine($"  Name: {result.Data.Get<string>("user_name")}");
        Console.WriteLine($"  Email: {result.Data.Get<string>("email")}");
         Console.WriteLine($"  Tier: {result.Data.Get<string>("subscription_tier")}");
                Console.WriteLine($"  Created: {result.Data.Get<string>("created_date")}");
            });

        var input = new WorkflowData()
    .Set("response", new
        {
      data = new
         {
    user = new
         {
                id = "user_12345",
         name = "Alice Johnson",
             contact = new { email = "alice@example.com", phone = "+1234567890" },
 createdAt = "2024-01-15T10:30:00Z"
     },
          subscription = new
        {
  plan = new { tier = "premium", price = 99.99 }
           }
      }
    });

        await workflow.RunAsync(input);

  // Default values example
  Console.WriteLine("\n  Default Values Example:");
        var defaultWorkflow = Workflow.Create("SafeMapper")
            .UseLogger(logger)
    .AddNode(new DataMapperNode(
name: "SafeMapping",
       mappings: new Dictionary<string, string>
         {
       ["country"] = "address.country",
              ["region"] = "address.region"
 },
                defaultValues: new Dictionary<string, object?>
      {
      ["country"] = "US",
   ["region"] = "Unknown"
      }
            ))
            .OnComplete(result =>
    {
    Console.WriteLine($"? Country: {result.Data.Get<string>("country")} (fallback used)");
     Console.WriteLine($"? Region: {result.Data.Get<string>("region")} (fallback used)");
          });

await defaultWorkflow.RunAsync(new WorkflowData().Set("address", new { city = "Seattle" }));
    }

    /// <summary>
    /// FilterNode: Validate data with custom rules
    /// Use Case: Input validation, data quality checks, business rules
    /// </summary>
    private static async Task FilterNodeExample()
{
        Console.WriteLine("\n??? 3. FilterNode Example ????????????????????????????????????");
        Console.WriteLine("Use Case: Validate user registration data\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("FilterExample");

        var workflow = Workflow.Create("UserRegistration")
       .UseLogger(logger)
       .AddNode(new FilterNode("ValidateInput")
   .RequireNonEmpty("email")
          .RequireNonEmpty("password")
                .MaxLength("password", 128)
    .Custom("email",
         data => data.GetString("email")?.Contains("@") == true,
            "Email must contain @")
     .Custom("password",
          data => (data.GetString("password")?.Length ?? 0) >= 8,
  "Password must be at least 8 characters")
.Custom("age",
   data => data.Get<int>("age") >= 18,
                    "User must be 18 or older"))
      .AddStep("ProcessRegistration", (data, ctx) =>
       {
ctx.Logger.LogInformation("? Validation passed - creating account");
  return Task.FromResult(data.Set("account_id", $"ACC-{Random.Shared.Next(10000, 99999)}"));
    })
 .OnComplete(result =>
            {
Console.WriteLine($"? Account created: {result.Data.GetString("account_id")}");
 });

        var input = new WorkflowData()
        .Set("email", "user@example.com")
    .Set("password", "SecurePass123!")
.Set("age", 25);

        await workflow.RunAsync(input);

        // Soft validation (doesn't throw)
     Console.WriteLine("\n  Soft Validation Example:");
   var softWorkflow = Workflow.Create("SoftValidation")
     .UseLogger(logger)
    .AddNode(new FilterNode("CheckOptional", throwOnFail: false)
     .RequireNonEmpty("company_name")
             .MaxLength("company_name", 100))
      .AddStep("HandleResult", (data, ctx) =>
     {
     var isValid = data.Get<bool>("is_valid");
                var errors = data.Get<List<string>>("validation_errors");
     if (!isValid)
  {
            ctx.Logger.LogWarning("Validation warnings: {Errors}", string.Join(", ", errors ?? new()));
       }
      return Task.FromResult(data);
      })
            .OnComplete(result =>
   {
        Console.WriteLine($"? Valid: {result.Data.Get<bool>("is_valid")}");
       if (!result.Data.Get<bool>("is_valid"))
    {
  var errors = result.Data.Get<List<string>>("validation_errors");
     Console.WriteLine($"? Errors: {string.Join("; ", errors ?? new())}");
          }
    });

      await softWorkflow.RunAsync(new WorkflowData()); // Missing company_name
    }

    /// <summary>
    /// TransformNode: Apply custom transformations to data
    /// Use Case: Data cleaning, formatting, calculations
    /// </summary>
    private static async Task TransformNodeExample()
    {
        Console.WriteLine("\n??? 4. TransformNode Example ?????????????????????????????????");
    Console.WriteLine("Use Case: Transform and clean user input\n");

     using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("TransformExample");

        var workflow = Workflow.Create("DataCleaning")
      .UseLogger(logger)
 .AddNode(new TransformNode("CleanData", data =>
     {
      var name = data.GetString("name")?.Trim().ToUpper() ?? "";
     var email = data.GetString("email")?.Trim().ToLower() ?? "";
       var phone = data.GetString("phone")?.Replace("-", "").Replace(" ", "") ?? "";

return data
   .Set("name", name)
    .Set("email", email)
      .Set("phone", phone)
    .Set("cleaned_at", DateTime.UtcNow.ToString("O"));
            }))
          .OnComplete(result =>
       {
            Console.WriteLine("? Cleaned Data:");
      Console.WriteLine($"  Name: {result.Data.GetString("name")}");
                Console.WriteLine($"  Email: {result.Data.GetString("email")}");
   Console.WriteLine($"  Phone: {result.Data.GetString("phone")}");
   Console.WriteLine($"  Cleaned at: {result.Data.GetString("cleaned_at")}");
            });

        var input = new WorkflowData()
      .Set("name", "  john doe  ")
  .Set("email", "  JOHN.DOE@EXAMPLE.COM")
 .Set("phone", "123-456-7890");

  await workflow.RunAsync(input);

      // Prebuilt transforms
        Console.WriteLine("\n  Prebuilt Transform Examples:");

      // Rename
        var renameWorkflow = Workflow.Create("Rename")
   .UseLogger(logger)
         .AddNode(TransformNode.Rename("old_field", "new_field"))
   .OnComplete(result =>
     {
    Console.WriteLine($"? Renamed: new_field = {result.Data.Get<string>("new_field")}");
       });
        await renameWorkflow.RunAsync(new WorkflowData().Set("old_field", "value"));

   // Concat strings
    var concatWorkflow = Workflow.Create("Concat")
        .UseLogger(logger)
     .AddNode(TransformNode.ConcatStrings("full_name", " ", "first_name", "last_name"))
  .OnComplete(result =>
            {
            Console.WriteLine($"? Concatenated: {result.Data.Get<string>("full_name")}");
     });
        await concatWorkflow.RunAsync(new WorkflowData()
       .Set("first_name", "John")
      .Set("last_name", "Doe"));
    }

    /// <summary>
    /// SetVariableNode: Set literal or interpolated values
    /// Use Case: Initialize variables, set defaults, create templates
    /// </summary>
    private static async Task SetVariableNodeExample()
    {
    Console.WriteLine("\n??? 5. SetVariableNode Example ???????????????????????????????");
        Console.WriteLine("Use Case: Initialize workflow variables and templates\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("SetVarExample");

        var workflow = Workflow.Create("WorkflowInit")
            .UseLogger(logger)
       .AddNode(new SetVariableNode("Initialize", new Dictionary<string, object?>
        {
      ["greeting"] = "Hello, {{user_name}}!",
      ["max_retries"] = 3,
 ["debug_mode"] = true,
       ["api_endpoint"] = "https://api.example.com/v1",
         ["timestamp"] = DateTime.UtcNow.ToString("O")
      }))
       .OnComplete(result =>
       {
   Console.WriteLine("? Variables Set:");
           Console.WriteLine($"  Greeting: {result.Data.GetString("greeting")}");
       Console.WriteLine($"  Max Retries: {result.Data.Get<int>("max_retries")}");
     Console.WriteLine($"  Debug: {result.Data.Get<bool>("debug_mode")}");
  Console.WriteLine($"  Endpoint: {result.Data.GetString("api_endpoint")}");
Console.WriteLine($"  Timestamp: {result.Data.GetString("timestamp")}");
            });

  var input = new WorkflowData()
        .Set("user_name", "Alice");

        await workflow.RunAsync(input);

        // Dynamic interpolation
        Console.WriteLine("\n  Template Interpolation Example:");
        var templateWorkflow = Workflow.Create("DynamicTemplate")
            .UseLogger(logger)
      .AddNode(new SetVariableNode("BuildMessage", new Dictionary<string, object?>
            {
      ["subject"] = "Order Confirmation - {{order_id}}",
    ["message"] = "Dear {{customer_name}}, your order {{order_id}} has been confirmed.",
   ["tracking_url"] = "https://tracking.example.com/{{order_id}}"
    }))
  .OnComplete(result =>
            {
     Console.WriteLine($"? Subject: {result.Data.GetString("subject")}");
            Console.WriteLine($"? Message: {result.Data.GetString("message")}");
          Console.WriteLine($"? Tracking: {result.Data.GetString("tracking_url")}");
   });

        await templateWorkflow.RunAsync(new WorkflowData()
          .Set("order_id", "ORD-98765")
   .Set("customer_name", "Bob Smith"));
    }

    /// <summary>
    /// MemoryNode: Read/write to workflow state (session memory)
    /// Use Case: Multi-turn conversations, stateful workflows, context persistence
    /// </summary>
    private static async Task MemoryNodeExample()
    {
     Console.WriteLine("\n??? 6. MemoryNode Example ????????????????????????????????????");
 Console.WriteLine("Use Case: Multi-turn conversation with state\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("MemoryExample");

        // First conversation turn - write to memory
   var firstTurnWorkflow = Workflow.Create("FirstTurn")
            .UseLogger(logger)
            .AddStep("ProcessInput", (data, ctx) =>
        {
  ctx.Logger.LogInformation("Processing first turn");
    return Task.FromResult(data
 .Set("user_preference", "dark_mode")
        .Set("session_start", DateTime.UtcNow.ToString("O"))
        .Set("conversation_count", 1));
         })
       .AddNode(MemoryNode.Write("user_preference", "session_start", "conversation_count"))
          .OnComplete(result =>
    {
         Console.WriteLine("? First turn - saved to memory:");
                Console.WriteLine($"  Preference: {result.Data.GetString("user_preference")}");
        Console.WriteLine($"  Session started: {result.Data.GetString("session_start")}");
 });

      var context = new WorkflowContext("ChatSession", logger);
     await firstTurnWorkflow.RunAsync(new WorkflowData(), context);

   // Second conversation turn - read from memory
Console.WriteLine("\n  Second Turn (reading from memory):");
    var secondTurnWorkflow = Workflow.Create("SecondTurn")
          .UseLogger(logger)
 .AddNode(MemoryNode.Read("user_preference", "session_start", "conversation_count"))
          .AddStep("UpdateCount", (data, ctx) =>
            {
      var count = data.Get<int>("conversation_count") + 1;
                return Task.FromResult(data.Set("conversation_count", count));
        })
   .AddNode(MemoryNode.Write("conversation_count"))
      .OnComplete(result =>
 {
      Console.WriteLine("? Second turn - loaded from memory:");
   Console.WriteLine($"  Preference: {result.Data.GetString("user_preference")}");
     Console.WriteLine($"  Session: {result.Data.GetString("session_start")}");
                Console.WriteLine($"  Turn count: {result.Data.Get<int>("conversation_count")}");
            });

  await secondTurnWorkflow.RunAsync(new WorkflowData(), context);

        // Third turn
        Console.WriteLine("\n  Third Turn:");
        await secondTurnWorkflow.RunAsync(new WorkflowData(), context);
    }
}
