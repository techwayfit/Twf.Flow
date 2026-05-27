using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Core.ValueObjects;
using Twf.Flow.Nodes.AI;

namespace twf_ai_framework.console.node_examples;

/// <summary>
/// Examples for all AI-category nodes:
/// - LlmNode
/// - EmbeddingNode
/// - PromptBuilderNode
/// - OutputParserNode
/// </summary>
public static class AINodeExamples
{
    public static async Task RunAllExamples(string apiKey)
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      AI NODE EXAMPLES            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        await LlmNodeExample(apiKey);
        await EmbeddingNodeExample(apiKey);
        await PromptBuilderNodeExample();
        await OutputParserNodeExample(apiKey);
    }

    /// <summary>
    /// LlmNode: Call any OpenAI-compatible LLM API
    /// Supports: OpenAI, Anthropic, Ollama, Azure OpenAI
    /// </summary>
    private static async Task LlmNodeExample(string apiKey)
    {
        Console.WriteLine("\n─── 1. LlmNode Example ───────────────────────────────────────");
        Console.WriteLine("Use Case: Call an LLM with a simple prompt\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("LlmExample");

        var workflow = WorkflowBuilder.Create("SimpleLLM")
         .UseLogger(logger)
     .AddNode(new LlmNode(
             name: "Summarizer",
         config: LlmConfig.Anthropic(apiKey, "claude-3-5-sonnet-20241022") with
         {
             Temperature = Temperature.FromValue(0.7f),
             MaxTokens = TokenCount.FromValue(200),
             DefaultSystemPrompt = "You are a helpful assistant. Be concise."
         }))
    .OnComplete(result =>
     {
         Console.WriteLine($"✓ Response: {result.Data.GetString("llm_response")?[..Math.Min(200, result.Data.GetString("llm_response")?.Length ?? 0)]}...");
         Console.WriteLine($"✓ Model: {result.Data.GetString("llm_model")}");
         Console.WriteLine($"✓ Tokens: {result.Data.Get<int>("prompt_tokens")} prompt + {result.Data.Get<int>("completion_tokens")} completion");
     });

        var input = new WorkflowData()
           .Set("prompt", "Explain what a workflow engine is in 2 sentences.");

        await workflow.RunAsync(input);
    }

    /// <summary>
    /// EmbeddingNode: Generate vector embeddings for text
    /// Use Case: RAG pipelines, semantic search, similarity matching
    /// </summary>
    private static async Task EmbeddingNodeExample(string apiKey)
    {
        Console.WriteLine("\n─── 2. EmbeddingNode Example ─────────────────────────────────");
        Console.WriteLine("Use Case: Generate embeddings for semantic search\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("EmbeddingExample");

        var workflow = WorkflowBuilder.Create("EmbeddingDemo")
            .UseLogger(logger)
  .AddNode(new EmbeddingNode(
          name: "Embedder",
                config: EmbeddingConfig.OpenAI(apiKey, "text-embedding-3-small")))
         .OnComplete(result =>
          {
              var embedding = result.Data.Get<float[]>("embedding");
              Console.WriteLine($"✓ Embedding dimensions: {embedding?.Length ?? 0}");
              Console.WriteLine($"✓ First 5 values: [{string.Join(", ", embedding?.Take(5).Select(v => v.ToString("F4")) ?? Array.Empty<string>())}]");
              Console.WriteLine($"✓ Model: {result.Data.GetString("embedding_model")}");
          });

        var input = new WorkflowData()
            .Set("text", "Twf.Flow is a workflow engine for building AI-powered applications.");

        await workflow.RunAsync(input);

        // Batch example
        Console.WriteLine("\n  Batch Embedding Example:");
        var batchWorkflow = WorkflowBuilder.Create("BatchEmbedding")
                  .UseLogger(logger)
                  .AddNode(new EmbeddingNode("BatchEmbedder", EmbeddingConfig.OpenAI(apiKey)))
      .OnComplete(result =>
             {
                 var embeddings = result.Data.Get<List<float[]>>("embeddings");
                 Console.WriteLine($"✓ Generated {embeddings?.Count ?? 0} embeddings");
                 if (embeddings != null && embeddings.Count > 0)
                 {
                     Console.WriteLine($"✓ Each with {embeddings[0].Length} dimensions");
                 }
             });

        var batchInput = new WorkflowData()
            .Set("texts", new List<string>
    {
           "Document 1: AI workflows are powerful",
        "Document 2: Workflow engines automate tasks",
      "Document 3: LLMs generate text"
            });

        await batchWorkflow.RunAsync(batchInput);
    }

    /// <summary>
    /// PromptBuilderNode: Build dynamic prompts with {{variable}} substitution
    /// Use Case: Template-based prompt generation
    /// </summary>
    private static async Task PromptBuilderNodeExample()
    {
        Console.WriteLine("\n─── 3. PromptBuilderNode Example ────────────────────────────");
        Console.WriteLine("Use Case: Build prompts from templates with variable substitution\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("PromptBuilderExample");

        var workflow = WorkflowBuilder.Create("DynamicPrompt")
            .UseLogger(logger)
            .AddNode(new PromptBuilderNode(
        name: "ChatPrompt",
    promptTemplate: "User question: {{question}}\n\nContext: {{context}}",
  systemTemplate: "You are a {{role}}. {{instructions}}"
        ))
   .OnComplete(result =>
         {
             Console.WriteLine($"✓ Generated Prompt:\n{result.Data.GetString("prompt")}");
             Console.WriteLine($"\n✓ System Prompt:\n{result.Data.GetString("system_prompt")}");
         });

        var input = new WorkflowData()
      .Set("question", "What is dependency injection?")
            .Set("context", ".NET uses built-in DI container")
   .Set("role", "senior software architect")
            .Set("instructions", "Explain concepts with code examples.");

        await workflow.RunAsync(input);

        // Static variables example
        Console.WriteLine("\n  Static Variables Example:");
        var staticWorkflow = WorkflowBuilder.Create("StaticTemplate")
         .UseLogger(logger)
            .AddNode(new PromptBuilderNode(
        name: "TranslatePrompt",
        promptTemplate: "Translate '{{text}}' to {{language}}",
       staticVariables: new Dictionary<string, object?>
       {
           ["language"] = "Spanish"  // Always Spanish
       }
            ))
     .OnComplete(result =>
     {
         Console.WriteLine($"✓ Prompt: {result.Data.GetString("prompt")}");
     });

        await staticWorkflow.RunAsync(new WorkflowData().Set("text", "Hello world"));
    }

    /// <summary>
    /// OutputParserNode: Extract structured JSON from LLM responses
    /// Use Case: Parse LLM outputs into structured data
    /// </summary>
    private static async Task OutputParserNodeExample(string apiKey)
    {
        Console.WriteLine("\n─── 4. OutputParserNode Example ─────────────────────────────");
        Console.WriteLine("Use Case: Extract structured data from LLM responses\n");

        using var logFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = logFactory.CreateLogger("ParserExample");

        var workflow = WorkflowBuilder.Create("StructuredExtraction")
            .UseLogger(logger)
            .AddNode(new LlmNode(
  name: "Extractor",
  config: LlmConfig.Anthropic(apiKey, "claude-3-5-sonnet-20241022") with
  {
      Temperature = Temperature.FromValue(0.3f),
      MaxTokens = TokenCount.FromValue(300)
  }))
            .AddNode(new OutputParserNode(
    name: "JSONParser",
          fieldMapping: new Dictionary<string, string>
          {
              ["sentiment"] = "user_sentiment",
              ["confidence"] = "confidence_score",
              ["topics"] = "detected_topics"
          }
        ))
     .OnComplete(result =>
      {
          Console.WriteLine($"✓ Sentiment: {result.Data.Get<string>("user_sentiment")}");
          Console.WriteLine($"✓ Confidence: {result.Data.Get<object>("confidence_score")}");
          Console.WriteLine($"✓ Topics: {string.Join(", ", result.Data.Get<List<object>>("detected_topics") ?? new())}");
      });

        var input = new WorkflowData()
            .Set("prompt", @"Analyze this review and return JSON with sentiment (positive/negative/neutral), 
confidence (0-1), and topics (array of strings):

'This product exceeded my expectations! The build quality is outstanding and customer service was very helpful.'

Return ONLY valid JSON.");

        await workflow.RunAsync(input);

        // Markdown fence example
        Console.WriteLine("\n  Markdown Code Fence Example:");
        var fenceWorkflow = WorkflowBuilder.Create("FencedJSON")
            .UseLogger(logger)
               .AddNode(new OutputParserNode("ParseFenced"))
        .OnComplete(result =>
      {
          Console.WriteLine($"✓ Extracted: {result.Data.Get<string>("summary")}");
          Console.WriteLine($"✓ Score: {result.Data.Get<object>("score")}");
      });

        var fencedInput = new WorkflowData()
   .Set("llm_response",
    "Here's the analysis:\n\n" +
         "```json\n" +
    "{\n" +
  "  \"summary\": \"Positive feedback on product quality\",\n" +
         "  \"score\": 9.2\n" +
       "}\n" +
     "```\n\n" +
     "The customer is satisfied.");

        await fenceWorkflow.RunAsync(fencedInput);
    }
}
