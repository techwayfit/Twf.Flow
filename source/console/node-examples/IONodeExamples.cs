using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.IO;

namespace twf_ai_framework.console.node_examples;

/// <summary>
/// Examples for all IO-category nodes:
/// - HttpRequestNode
/// - FileReaderNode
/// - FileWriterNode
/// - GoogleSearchNode
/// </summary>
public static class IONodeExamples
{
    public static async Task RunAllExamples(string? serpApiKey = null)
    {
        Console.WriteLine("\n??????????????????????????????????????????????????????????????");
        Console.WriteLine("?      I/O NODE EXAMPLES    ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????\n");

        await HttpRequestNodeExample();
      await FileReaderNodeExample();
        await FileWriterNodeExample();
        if (!string.IsNullOrEmpty(serpApiKey))
        {
         await GoogleSearchNodeExample(serpApiKey);
    }
      else
  {
          Console.WriteLine("\n??? 4. GoogleSearchNode Example (SKIPPED) ????????????????????");
       Console.WriteLine("??  SerpApi key not provided. Get free key at https://serpapi.com\n");
        }
    }

    /// <summary>
    /// HttpRequestNode: Make HTTP/REST API calls
    /// Use Case: External API integration, webhooks, data fetching
    /// </summary>
    private static async Task HttpRequestNodeExample()
    {
        Console.WriteLine("\n??? 1. HttpRequestNode Example ???????????????????????????????");
        Console.WriteLine("Use Case: Fetch user data from JSONPlaceholder API\n");

  using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("HttpExample");

        // GET request
        var workflow = WorkflowBuilder.Create("FetchUser")
            .UseLogger(logger)
            .AddNode(HttpRequestNode.Get(
     name: "GetUser",
                url: "https://jsonplaceholder.typicode.com/users/{{user_id}}"))
            .OnComplete(result =>
            {
    var response = result.Data.Get<object>("http_response");
     var statusCode = result.Data.Get<int>("http_status_code");
       Console.WriteLine($"? Status: {statusCode}");
          Console.WriteLine($"? Response: {System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
      });

  var input = new WorkflowData().Set("user_id", "1");
  await workflow.RunAsync(input);

        // POST request
  Console.WriteLine("\n  POST Request Example:");
        var postWorkflow = WorkflowBuilder.Create("CreatePost")
          .UseLogger(logger)
            .AddNode(HttpRequestNode.Post(
    name: "CreatePost",
      url: "https://jsonplaceholder.typicode.com/posts",
     body: new
   {
       title = "Test Post",
         body = "This is a test post created by Twf.Flow",
  userId = 1
      }))
     .OnComplete(result =>
  {
      var response = result.Data.Get<object>("http_response");
    Console.WriteLine($"? Created: {System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
 });

        await postWorkflow.RunAsync(new WorkflowData());

        // Custom headers
        Console.WriteLine("\n  Custom Headers Example:");
        var headerWorkflow = WorkflowBuilder.Create("WithHeaders")
     .UseLogger(logger)
    .AddNode(new HttpRequestNode(
    name: "AuthenticatedRequest",
                config: new HttpRequestConfig
        {
       Method = "GET",
       UrlTemplate = "https://jsonplaceholder.typicode.com/posts/1",
          Headers = new Dictionary<string, string>
     {
          ["User-Agent"] = "Twf.Flow/1.0",
            ["Accept"] = "application/json"
              },
      Timeout = TimeSpan.FromSeconds(10)
            }))
            .OnComplete(result =>
  {
        var headers = result.Data.Get<Dictionary<string, string>>("http_headers");
    Console.WriteLine($"? Response headers: {string.Join(", ", headers?.Keys ?? Enumerable.Empty<string>())}");
    });

        await headerWorkflow.RunAsync(new WorkflowData());
    }

    /// <summary>
    /// FileReaderNode: Read files from disk
    /// Use Case: Load documents, config files, templates
    /// </summary>
    private static async Task FileReaderNodeExample()
    {
        Console.WriteLine("\n??? 2. FileReaderNode Example ????????????????????????????????");
        Console.WriteLine("Use Case: Load document for processing\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
      var logger = logFactory.CreateLogger("FileReaderExample");

   // Create a test file
    var testFilePath = Path.Combine(Path.GetTempPath(), "twf_test_doc.txt");
   await File.WriteAllTextAsync(testFilePath,
    "Twf.Flow Documentation\n" +
     "============================\n\n" +
     "This is a sample document for demonstrating the FileReaderNode.\n" +
   "It contains multiple paragraphs and can be processed by the framework.\n\n" +
            "Features:\n" +
  "- Workflow orchestration\n" +
     "- AI integration\n" +
"- Data transformation\n" +
         "- Error handling");

        var workflow = WorkflowBuilder.Create("LoadDocument")
            .UseLogger(logger)
.AddNode(new FileReaderNode(testFilePath))
         .OnComplete(result =>
 {
       var content = result.Data.GetString("text");
             var fileName = result.Data.GetString("file_name");
      var fileSize = result.Data.Get<long>("file_size");
                var extension = result.Data.GetString("file_extension");

   Console.WriteLine($"? File: {fileName}");
   Console.WriteLine($"? Size: {fileSize} bytes");
      Console.WriteLine($"? Extension: {extension}");
   Console.WriteLine($"? Content preview:\n{content?[..Math.Min(150, content?.Length ?? 0)]}...");
});

        await workflow.RunAsync(new WorkflowData());

        // Dynamic path example
        Console.WriteLine("\n  Dynamic File Path Example:");
   var dynamicWorkflow = WorkflowBuilder.Create("DynamicLoader")
 .UseLogger(logger)
         .AddNode(new FileReaderNode())
   .OnComplete(result =>
 {
       Console.WriteLine($"? Loaded: {result.Data.GetString("file_name")} ({result.Data.Get<long>("file_size")} bytes)");
       });

        var dynamicInput = new WorkflowData().Set("file_path", testFilePath);
  await dynamicWorkflow.RunAsync(dynamicInput);

        // Cleanup
        File.Delete(testFilePath);
    }

    /// <summary>
    /// FileWriterNode: Write data to files
    /// Use Case: Save reports, export results, generate artifacts
    /// </summary>
    private static async Task FileWriterNodeExample()
    {
Console.WriteLine("\n??? 3. FileWriterNode Example ????????????????????????????????");
    Console.WriteLine("Use Case: Save generated report to file\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("FileWriterExample");

        var outputPath = Path.Combine(Path.GetTempPath(), "twf_output_report.txt");

   var workflow = WorkflowBuilder.Create("GenerateReport")
            .UseLogger(logger)
        .AddStep("CreateReport", (data, ctx) =>
        {
var report = "Twf.Flow Execution Report\n" +
         "================================\n" +
     $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
      $"Workflow: {data.GetString("workflow_name")}\n" +
       "Status: Success\n\n" +
  "Summary:\n" +
     $"- Total steps: {data.Get<int>("step_count")}\n" +
       $"- Duration: {data.Get<double>("duration_ms")}ms\n" +
        $"- Data processed: {data.Get<int>("records_processed")} records\n\n" +
       "Results:\n" +
       $"{data.GetString("results")}";

   return Task.FromResult(data.Set("report_content", report));
     })
  .AddNode(new FileWriterNode(outputPath, "report_content"))
        .OnComplete(result =>
            {
            var outputFile = result.Data.GetString("output_file");
                Console.WriteLine($"? Report saved to: {outputFile}");
                Console.WriteLine($"? File exists: {File.Exists(outputFile)}");
            });

     var input = new WorkflowData()
            .Set("workflow_name", "DataProcessingPipeline")
  .Set("step_count", 5)
 .Set("duration_ms", 1234.56)
            .Set("records_processed", 100)
   .Set("results", "All records processed successfully");

    await workflow.RunAsync(input);

        // Read it back to verify
        Console.WriteLine("\n  Verifying written file:");
if (File.Exists(outputPath))
        {
      var content = await File.ReadAllTextAsync(outputPath);
            Console.WriteLine($"? File content preview:\n{content[..Math.Min(200, content.Length)]}...");
        File.Delete(outputPath); // Cleanup
        }

        // Template path with variables
        Console.WriteLine("\n  Template Path Example:");
        var templateWorkflow = WorkflowBuilder.Create("TemplateOutput")
    .UseLogger(logger)
  .AddStep("PrepareData", (data, ctx) =>
            {
    return Task.FromResult(data
              .Set("llm_response", "This is the generated content from the LLM.")
          .Set("request_id", $"req_{DateTime.UtcNow:yyyyMMdd_HHmmss}"));
            })
  .AddNode(new FileWriterNode(
       Path.Combine(Path.GetTempPath(), "output_{{request_id}}.txt"),
             "llm_response"))
     .OnComplete(result =>
            {
  Console.WriteLine($"? Saved to: {result.Data.GetString("output_file")}");
    var path = result.Data.GetString("output_file");
        if (path != null && File.Exists(path))
      {
   File.Delete(path); // Cleanup
        }
   });

        await templateWorkflow.RunAsync(new WorkflowData());
 }

    /// <summary>
    /// GoogleSearchNode: Search Google via SerpApi
    /// Use Case: Web research, data enrichment, RAG context
    /// </summary>
    private static async Task GoogleSearchNodeExample(string apiKey)
    {
        Console.WriteLine("\n??? 4. GoogleSearchNode Example ??????????????????????????????");
     Console.WriteLine("Use Case: Search for information to enrich LLM context\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
    var logger = logFactory.CreateLogger("SearchExample");

   var workflow = WorkflowBuilder.Create("WebResearch")
      .UseLogger(logger)
            .AddNode(new GoogleSearchNode(apiKey))
            .OnComplete(result =>
          {
  var results = result.Data.Get<List<SearchResultItem>>("search_results") ?? new();
           var query = result.Data.GetString("search_query_used");
                var count = result.Data.Get<int>("search_results_count");

        Console.WriteLine($"? Query: {query}");
 Console.WriteLine($"? Results: {count}\n");

      for (int i = 0; i < Math.Min(3, results.Count); i++)
{
              Console.WriteLine($"  {i + 1}. {results[i].Title}");
           Console.WriteLine($"     {results[i].Description[..Math.Min(100, results[i].Description.Length)]}...");
             Console.WriteLine($"     {results[i].LinkedPage}");
  Console.WriteLine();
       }

      if (results.Count > 3)
       {
        Console.WriteLine($"  ... and {results.Count - 3} more results");
       }
            });

        var input = new WorkflowData()
            .Set("search_query", "Twf.Flow .NET workflow engine")
        .Set("search_results_count", 5);

        try
        {
     await workflow.RunAsync(input);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"??  Search failed: {ex.Message}");
            Console.WriteLine("   Check your SerpApi key and quota at https://serpapi.com");
        }

        // Custom result count example
        Console.WriteLine("\n  Custom Result Count Example:");
        var limitedWorkflow = WorkflowBuilder.Create("LimitedSearch")
            .UseLogger(logger)
         .AddNode(new GoogleSearchNode(apiKey))
            .OnComplete(result =>
     {
 var count = result.Data.Get<int>("search_results_count");
       Console.WriteLine($"? Retrieved {count} results (limited)");
    });

        var limitedInput = new WorkflowData()
            .Set("search_query", "artificial intelligence trends 2024")
   .Set("search_results_count", 3);

        try
        {
            await limitedWorkflow.RunAsync(limitedInput);
        }
     catch (Exception ex)
  {
Console.WriteLine($"??  Search failed: {ex.Message}");
        }
    }
}
