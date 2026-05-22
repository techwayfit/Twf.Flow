# Twf.Flow  How-To Guide

Complete guide to common tasks and workflows in Twf.Flow. Each section provides step-by-step instructions with code examples.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Building Your First Workflow](#2-building-your-first-workflow)
3. [Working with LLMs](#3-working-with-llms)
4. [Data Transformation](#4-data-transformation)
5. [Control Flow](#5-control-flow)
6. [Error Handling](#6-error-handling)
7. [Working with External APIs](#7-working-with-external-apis)
8. [RAG and Embeddings](#8-rag-and-embeddings)
9. [Multi-Turn Conversations](#9-multi-turn-conversations)
10. [Testing Workflows](#10-testing-workflows)
11. [Performance Optimization](#11-performance-optimization)
12. [Deployment](#12-deployment)

---

## 1. Getting Started

### Install the Package

```bash
dotnet add package Twf.Flow
```

### Basic Setup

```csharp
using Microsoft.Extensions.Logging;
using Twf.Flow.Core;
using Twf.Flow.Nodes.AI;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("MyWorkflow");

// Create your first workflow
var workflow = Workflow.Create("HelloWorld")
    .UseLogger(logger);
```

### Configuration Best Practices

**Store API keys securely:**

```csharp
//  DON'T: Hardcode API keys
var llmConfig = LlmConfig.OpenAI("sk-hardcoded-key", "gpt-4o");

//  DO: Use environment variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    throw new InvalidOperationException("API key not configured");
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o");

//  BETTER: Use configuration
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var apiKey = configuration["OpenAI:ApiKey"];
```

**Use dependency injection:**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IHttpClientProvider, PooledHttpClientProvider>();
services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
services.AddTransient<LlmNode>(sp => new LlmNode(
    "MyLLM",
    llmConfig,
    sp.GetRequiredService<IHttpClientProvider>(),
    sp.GetRequiredService<ISecretProvider>()
));
```

---

## 2. Building Your First Workflow

### Simple Sequential Workflow

**Task:** Call an LLM to answer a question.

```csharp
var workflow = Workflow.Create("SimpleQA")
    .UseLogger(logger)
    
    // Step 1: Build the prompt
    .AddNode(new PromptBuilderNode(
        name: "BuildPrompt",
        promptTemplate: "Answer this question clearly: {{question}}",
        systemTemplate: "You are a helpful assistant."
    ))
 
    // Step 2: Call the LLM
    .AddNode(new LlmNode("GetAnswer", LlmConfig.OpenAI(apiKey, "gpt-4o")))
    
    // Step 3: Log the result
    .AddNode(LogNode.Keys("Result", "llm_response"));

// Execute
var input = WorkflowData.From("question", "What is the capital of France?");
var result = await workflow.RunAsync(input);

if (result.IsSuccess)
{
    Console.WriteLine($"Answer: {result.Data.GetString("llm_response")}");
}
```

### Adding Validation

**Add input validation:**

```csharp
var workflow = Workflow.Create("ValidatedQA")
    .UseLogger(logger)
 
    // Validate input before processing
    .AddNode(new FilterNode("ValidateInput")
        .RequireNonEmpty("question")
        .MaxLength("question", 500))
    
    .AddNode(new PromptBuilderNode("BuildPrompt", /* ... */))
    .AddNode(new LlmNode("GetAnswer", llmConfig))
    
    .OnError((nodeName, exception) =>
    {
        Console.WriteLine($" Failed at {nodeName}: {exception?.Message}");
    });
```

### Using Inline Steps

**For simple transformations, use lambda nodes:**

```csharp
var workflow = Workflow.Create("QuickTransform")
    .AddStep("Uppercase", (data, ctx) =>
    {
        var text = data.GetString("input");
        return Task.FromResult(data.Set("output", text?.ToUpperInvariant()));
    })
    
.AddStep("AddTimestamp", (data, ctx) =>
    {
   return Task.FromResult(data.Set("timestamp", DateTime.UtcNow));
    });
```

---

## 3. Working with LLMs

### OpenAI Configuration

```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o") with
{
    DefaultSystemPrompt = "You are a professional assistant.",
    Temperature = Temperature.FromValue(0.7f),    // 0.0 (deterministic) to 2.0 (creative)
    MaxTokens = TokenCount.FromValue(2000),
    MaintainHistory = false
};

var llmNode = new LlmNode("MyLLM", llmConfig);
```

### Anthropic (Claude) Configuration

```csharp
var llmConfig = LlmConfig.Anthropic(apiKey, "claude-3-5-sonnet-20241022") with
{
    Temperature = Temperature.FromValue(0.5f),
    MaxTokens = TokenCount.FromValue(1500)
};
```

### Azure OpenAI Configuration

```csharp
var llmConfig = LlmConfig.AzureOpenAI(
    apiKey: azureApiKey,
    model: "gpt-4o",
    endpoint: "https://your-resource.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2024-08-01-preview"
);
```

### Ollama (Local) Configuration

```csharp
var llmConfig = LlmConfig.Ollama(
    model: "llama3.2",
    host: "http://localhost:11434"  // Default Ollama endpoint
);
```

### Streaming Responses

**Real-time token streaming with callbacks:**

```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o") with
{
    Stream = true,
    OnChunk = chunk => Console.Write(chunk)  // Invoked per text chunk
};

var workflow = Workflow.Create("StreamingChat")
    .AddNode(new PromptBuilderNode("Prompt", "{{question}}"))
    .AddNode(new LlmNode("StreamingLLM", llmConfig));

var result = await workflow.RunAsync(
    WorkflowData.From("question", "Tell me a story about AI.")
);
// Tokens appear in console as they're received
```

### Controlling Temperature

```csharp
// Deterministic (factual Q&A, math, code)
Temperature = Temperature.Deterministic  // 0.0

// Balanced (default, general purpose)
Temperature = Temperature.Balanced       // 0.7

// Creative (storytelling, brainstorming)
Temperature = Temperature.Creative       // 1.2

// Custom
Temperature = Temperature.FromValue(0.9f)
```

### Token Management

```csharp
// Set limits
var llmConfig = llmConfig with
{
    MaxTokens = TokenCount.FromValue(500)
};

// Track usage
var result = await workflow.RunAsync(input);
var promptTokens = result.Data.Get<int>("prompt_tokens");
var completionTokens = result.Data.Get<int>("completion_tokens");
var totalCost = CalculateCost(promptTokens, completionTokens, "gpt-4o");
```

### Prompt Engineering Best Practices

```csharp
//  DO: Be specific and structured
var prompt = new PromptBuilderNode("Prompt",
    promptTemplate: """
        Classify the sentiment of this customer message.
        
     Message: "{{message}}"
        
        Respond ONLY with valid JSON:
     {
          "sentiment": "positive|neutral|negative|angry",
          "confidence": 0.0-1.0,
       "reason": "brief explanation"
        }
        """,
    systemTemplate: "You are a sentiment analysis expert. Always return valid JSON."
);

//  DON'T: Vague prompts
var badPrompt = new PromptBuilderNode("Prompt",
    promptTemplate: "What do you think about: {{message}}"
);
```

### Parsing LLM Outputs

```csharp
// Option 1: OutputParserNode (recommended for JSON)
.AddNode(new LlmNode("Classifier", llmConfig))
.AddNode(new OutputParserNode("ParseJSON", new Dictionary<string, string>
{
    ["sentiment"] = "sentiment",
    ["confidence"] = "confidence_score"
}))

// Option 2: Manual parsing with inline step
.AddNode(new LlmNode("Classifier", llmConfig))
.AddStep("ManualParse", (data, ctx) =>
{
    var response = data.GetString("llm_response");
    // Custom parsing logic
    var parsed = JsonSerializer.Deserialize<MyModel>(response);
    return Task.FromResult(data.Set("parsed", parsed));
})
```

---

## 4. Data Transformation

### Renaming Keys

```csharp
// Using TransformNode
.AddNode(TransformNode.Rename("old_name", "new_name"))

// Using DataMapperNode
.AddNode(new DataMapperNode("Mapper", new Dictionary<string, string>
{
    { "new_name", "old_name" }
}))

// Using inline step
.AddStep("Rename", (data, _) =>
{
    var value = data.Get<string>("old_name");
    return Task.FromResult(data.Remove("old_name").Set("new_name", value));
})
```

### Extracting Nested Values

```csharp
// From HTTP response JSON
var mapper = new DataMapperNode("ExtractData", new Dictionary<string, string>
{
    { "customer_id", "http_response.data.customer.id" },
    { "customer_name", "http_response.data.customer.name" },
    { "email", "http_response.data.customer.email" }
});

// With default fallbacks
var mapper = new DataMapperNode("ExtractData", 
  mappings: new Dictionary<string, string>
    {
        { "customer_id", "http_response.data.customer.id" }
    },
    defaultValues: new Dictionary<string, object?>
    {
      { "customer_id", "UNKNOWN" }
    }
);
```

### Combining Multiple Fields

```csharp
// Concatenate strings
.AddNode(TransformNode.ConcatStrings(
    outputKey: "full_name",
    separator: " ",
    inputKeys: "first_name", "last_name"
))

// Custom combination
.AddStep("BuildAddress", (data, _) =>
{
    var street = data.GetString("street");
    var city = data.GetString("city");
    var zip = data.GetString("zip");
    var address = $"{street}, {city} {zip}";
    return Task.FromResult(data.Set("full_address", address));
})
```

### Filtering and Validation

```csharp
// Basic validation
var filter = new FilterNode("Validate")
    .RequireNonEmpty("email")
    .MaxLength("message", 2000)
    .MinLength("password", 8);

// Custom conditions
var filter = new FilterNode("ValidateAge")
    .AddCondition("age", value => 
    {
  var age = Convert.ToInt32(value);
   return age >= 18 && age <= 120;
    }, "Age must be between 18 and 120");

// Strict vs non-strict mode
var strictFilter = new FilterNode("StrictValidate", strict: true);
// Throws exception on validation failure

var softFilter = new FilterNode("SoftValidate", strict: false);
// Sets "is_valid" flag, continues execution
```

### Type Conversions

```csharp
.AddStep("ConvertTypes", (data, _) =>
{
    // String to int
    var ageStr = data.GetString("age");
    var age = int.Parse(ageStr);
    
    // String to DateTime
  var dateStr = data.GetString("created_date");
    var date = DateTime.Parse(dateStr);
    
    // JSON string to object
    var jsonStr = data.GetString("json_data");
    var obj = JsonSerializer.Deserialize<MyModel>(jsonStr);
    
    return Task.FromResult(data
        .Set("age", age)
.Set("created_date", date)
        .Set("parsed_data", obj));
})
```

---

## 5. Control Flow

### Conditional Branching

```csharp
// Using Workflow.Branch
var workflow = Workflow.Create("ConditionalFlow")
    .AddNode(new ConditionNode("CheckScore",
  ("is_high_score", data => data.Get<int>("score") > 80)))
    
    .Branch(
 condition: data => data.Get<bool>("is_high_score"),
        trueBranch: high => high
    .AddStep("Celebrate", (data, _) => 
  Task.FromResult(data.Set("message", "Congratulations!"))),
        falseBranch: low => low
     .AddStep("Encourage", (data, _) => 
     Task.FromResult(data.Set("message", "Keep trying!")))
  );

// Using BranchNode (switch/case)
var branchNode = new BranchNode("RouteByIntent", "intent",
    new("greeting", greetingWorkflow),
  new("complaint", complaintWorkflow),
    new("billing", billingWorkflow)
);

workflow.AddNode(branchNode);
// Execution routes to one of the workflows based on "intent" value
```

### Parallel Execution

```csharp
// Run multiple nodes concurrently
.Parallel(
    new SentimentAnalysisNode("Sentiment"),
    new KeywordExtractorNode("Keywords"),
    new CategoryClassifierNode("Category"),
  new SpamDetectorNode("Spam")
)
// Results merged into single WorkflowData

// With error handling
.Parallel(
    new Node1(),
    new Node2(),
    new Node3()
)
.AddStep("CheckResults", (data, ctx) =>
{
    // Check which parallel nodes succeeded
    var hasNode1Result = data.Has("node1_output");
    var hasNode2Result = data.Has("node2_output");
    return Task.FromResult(data);
})
```

### Loops and Iteration

```csharp
// Process a list of items
var workflow = Workflow.Create("ProcessList")
    // Assume "items" is a List<string>
    .ForEach(
        itemsKey: "items",
        outputKey: "processed_items",
        bodyBuilder: loop => loop
    .AddStep("ProcessItem", (data, _) =>
 {
        var item = data.GetString("__loop_item__");  // Current item
       var processed = item.ToUpperInvariant();
            return Task.FromResult(data.Set("result", processed));
   })
            .AddNode(new LlmNode("Enhance", llmConfig))  // Could call LLM per item
    );
// "processed_items" will contain a List<WorkflowData> of all results

// Example: Process documents
var documents = new List<string> { "doc1.txt", "doc2.txt", "doc3.txt" };
var input = WorkflowData.From("files", documents);

.ForEach(
    itemsKey: "files",
    outputKey: "summaries",
    bodyBuilder: doc => doc
        .AddNode(new FileReaderNode("Read"))
        .AddNode(new PromptBuilderNode("SummarizePrompt", "Summarize: {{content}}"))
      .AddNode(new LlmNode("Summarizer", llmConfig))
);
```

### Conditional Execution (Skip Nodes)

```csharp
// Skip node based on condition
var options = new NodeOptions
{
    RunCondition = data => data.GetString("environment") == "production"
};

.AddNode(new EmailNotificationNode("Notify"), options);
// Only runs in production

// Skip with feature flags
var options = new NodeOptions
{
    RunCondition = data => data.Get<bool>("feature_enabled") == true
};
```

### Try-Catch Patterns

```csharp
var workflow = Workflow.Create("SafeFlow")
    .AddNode(new TryCatchNode("SafeAPICall",
        tryBuilder: t => t
            .AddNode(new HttpRequestNode("CallAPI", apiConfig))
   .AddNode(new LlmNode("ProcessResponse", llmConfig)),
      
   catchBuilder: c => c
            .AddStep("LogError", (data, ctx) =>
            {
             var error = data.GetString("caught_error_message");
           ctx.Logger.LogError("API call failed: {Error}", error);
      return Task.FromResult(data);
   })
         .AddStep("UseFallback", (data, _) =>
   Task.FromResult(data.Set("response", "Fallback response")))
    ));
```

---

## 6. Error Handling

### Retry Logic

```csharp
// Basic retry
.AddNode(new HttpRequestNode("CallAPI", apiConfig),
 NodeOptions.WithRetry(maxRetries: 3))

// Retry with backoff
.AddNode(new LlmNode("LLMCall", llmConfig),
    NodeOptions.WithRetry(
        maxRetries: 3,
   retryDelay: TimeSpan.FromSeconds(2)  // Exponential: 2s, 4s, 8s
    ))

// Retry with timeout
.AddNode(new HttpRequestNode("SlowAPI", apiConfig),
    NodeOptions.WithRetry(3, TimeSpan.FromSeconds(1))
  .AndTimeout(TimeSpan.FromSeconds(30)))
```

### Continue on Error

```csharp
// Don't stop workflow if this node fails
.AddNode(new OptionalAnalyticsNode("Analytics"),
    new NodeOptions { ContinueOnError = true })

// With fallback data
.AddNode(new ExternalAPINode("GetData"),
    new NodeOptions
    {
     ContinueOnError = true,
        FallbackData = new Dictionary<string, object?>
        {
        { "data", new { status = "unavailable" } }
        }
    })
```

### Workflow-Level Error Handling

```csharp
var workflow = Workflow.Create("SafeWorkflow")
    .ContinueOnErrors()  // Never stop, even on failures
    
    .OnError((nodeName, exception) =>
    {
        // Log to monitoring service
logger.LogError(exception, "Node {NodeName} failed", nodeName);
        SendToSentry(exception, nodeName);
    })
    
    .OnComplete(result =>
    {
        if (result.IsFailure)
        {
            SendAlert($"Workflow failed: {result.ErrorMessage}");
        }
    });
```

### Custom Error Routes

```csharp
.AddNode(new TryCatchNode("ProtectedOperation",
    tryBuilder: t => t.AddNode(riskyNode),
    catchBuilder: c => c
        .AddNode(new ConditionNode("ClassifyError",
       ("is_timeout", data => 
 data.GetString("caught_exception_type")?.Contains("Timeout") == true),
            ("is_network", data => 
        data.GetString("caught_exception_type")?.Contains("Http") == true)
        ))
        .Branch(
     condition: data => data.Get<bool>("is_timeout"),
  trueBranch: timeout => timeout
       .AddStep("RetryLater", (data, _) => 
            Task.FromResult(data.Set("action", "retry_scheduled"))),
      falseBranch: other => other
          .AddStep("LogAndNotify", (data, _) =>
      {
        SendAlert($"Unexpected error: {data.GetString("caught_error_message")}");
   return Task.FromResult(data);
 })
        )
))
```

### Validation and Early Exit

```csharp
var workflow = Workflow.Create("ValidatedPipeline")
    // Validate early
    .AddNode(new FilterNode("ValidateInput")
        .RequireNonEmpty("user_id")
        .RequireNonEmpty("action")
        .AddCondition("action", value => 
          new[] { "create", "update", "delete" }.Contains(value as string),
    "Action must be create, update, or delete"))
    
    // If validation fails, FilterNode throws and workflow stops
    // To handle gracefully:
    .AddNode(new TryCatchNode("SafeValidation",
        tryBuilder: t => t.AddNode(validationNode),
        catchBuilder: c => c
       .AddStep("ReturnError", (data, _) =>
        Task.FromResult(data.Set("error", "Invalid input")))
    ));
```

---

## 7. Working with External APIs

### HTTP GET Requests

```csharp
var httpConfig = new HttpRequestConfig
{
    Method = "GET",
    UrlTemplate = "https://api.example.com/users/{{user_id}}",
    Headers = new Dictionary<string, string>
    {
    ["Authorization"] = "Bearer {{api_token}}",
        ["Accept"] = "application/json"
    },
    Timeout = TimeSpan.FromSeconds(30),
    ThrowOnError = true
};

var workflow = Workflow.Create("FetchUser")
    .AddNode(new HttpRequestNode("GetUser", httpConfig))
    
    .AddStep("ProcessResponse", (data, _) =>
    {
    var response = data.Get<Dictionary<string, object>>("http_response");
        var name = response?["name"] as string;
        return Task.FromResult(data.Set("user_name", name));
    });

var input = WorkflowData.From("user_id", "12345")
  .Set("api_token", apiToken);
var result = await workflow.RunAsync(input);
```

### HTTP POST Requests

```csharp
var createUserConfig = new HttpRequestConfig
{
    Method = "POST",
    UrlTemplate = "https://api.example.com/users",
Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer {{api_token}}",
        ["Content-Type"] = "application/json"
    },
    Body = new
    {
        name = "{{user_name}}",
        email = "{{user_email}}",
        role = "{{user_role}}"
    }
};

var workflow = Workflow.Create("CreateUser")
    .AddNode(new HttpRequestNode("CreateUser", createUserConfig));
```

### Dynamic Request Bodies

```csharp
var workflow = Workflow.Create("DynamicAPI")
    // Build request body dynamically
    .AddStep("PrepareRequest", (data, _) =>
    {
   var requestBody = new
   {
            action = data.GetString("action"),
            payload = new
 {
     id = data.GetString("id"),
          timestamp = DateTime.UtcNow,
        data = data.Get<object>("data")
            }
        };
   return Task.FromResult(data.Set("request_body", requestBody));
    })
    
    .AddNode(new HttpRequestNode("CallAPI", new HttpRequestConfig
    {
        Method = "POST",
        UrlTemplate = "https://api.example.com/actions",
  // Body will be read from "request_body" key in WorkflowData
    }));
```

### Handling API Rate Limits

```csharp
var workflow = Workflow.Create("RateLimited")
    .AddNode(new HttpRequestNode("API1", config1))
    .AddNode(new DelayNode(TimeSpan.FromMilliseconds(200)))  // Rate limit pause
    .AddNode(new HttpRequestNode("API2", config2))
    .AddNode(new DelayNode(TimeSpan.FromMilliseconds(200)))
    .AddNode(new HttpRequestNode("API3", config3));

// Or with a helper
.AddNode(DelayNode.RateLimitDelay(requestsPerMinute: 60))
```

### Error Handling for APIs

```csharp
var apiConfig = new HttpRequestConfig
{
    Method = "GET",
  UrlTemplate = "https://unreliable-api.com/data",
    ThrowOnError = false  // Don't throw, handle status codes manually
};

var workflow = Workflow.Create("ResilientAPI")
    .AddNode(new HttpRequestNode("CallAPI", apiConfig),
        NodeOptions.WithRetry(3, TimeSpan.FromSeconds(2)))
    
    .AddStep("CheckStatus", (data, _) =>
    {
        var statusCode = data.Get<int>("http_status_code");
        if (statusCode >= 400)
        {
        return Task.FromResult(data.Set("api_error", true));
     }
        return Task.FromResult(data.Set("api_error", false));
    })
    
    .Branch(
        condition: data => data.Get<bool>("api_error"),
    trueBranch: error => error
     .AddStep("UseCachedData", (data, _) =>
           Task.FromResult(data.Set("data", cachedData))),
        falseBranch: success => success
            .AddStep("ProcessData", (data, _) =>
                Task.FromResult(data.Set("processed", processedData)))
    );
```

---

## 8. RAG and Embeddings

### Document Chunking

```csharp
var workflow = Workflow.Create("ChunkDocuments")
    .AddNode(new FileReaderNode("ReadDoc", "path/to/document.txt"))
    
    .AddNode(new ChunkTextNode(new ChunkConfig
    {
        ChunkSize = ChunkSize.FromValue(500),        // 500 chars per chunk
 Overlap = ChunkOverlap.FromValue(100),       // 100 char overlap
  Strategy = ChunkStrategy.Sentence  // Split on sentences
    }));

// Result: "chunks" key contains List<TextChunk>
// Each chunk has: Text, Source, Index, StartPos, EndPos
```

### Generating Embeddings

```csharp
var embeddingConfig = new EmbeddingConfig
{
    Model = "text-embedding-3-small",
    ApiKey = apiKey,
    ApiUrl = "https://api.openai.com/v1/embeddings"
};

// Single text embedding
var workflow = Workflow.Create("EmbedText")
.AddStep("PrepareText", (data, _) =>
        Task.FromResult(data.Set("text", "This is the text to embed")))
    .AddNode(new EmbeddingNode("Embedder", embeddingConfig));

// Result: "embedding" key contains float[]

// Batch embedding (in a loop)
.ForEach(
    itemsKey: "chunks",  // List<TextChunk>
  outputKey: "embedded_chunks",
    bodyBuilder: loop => loop
  .AddStep("ExtractText", (data, _) =>
        {
        var chunk = data.Get<TextChunk>("__loop_item__");
            return Task.FromResult(data.Set("text", chunk.Text));
      })
        .AddNode(new EmbeddingNode("ChunkEmbedder", embeddingConfig))
        .AddNode(new DelayNode(TimeSpan.FromMilliseconds(100)))  // Rate limiting
);
```

### Vector Search (Semantic Similarity)

```csharp
// In-memory vector search example
var vectorStore = new List<(string Text, float[] Embedding)>();

// Store embeddings
.AddStep("StoreEmbedding", (data, _) =>
{
    var text = data.GetString("text");
    var embedding = data.Get<float[]>("embedding");
  vectorStore.Add((text, embedding));
    return Task.FromResult(data);
})

// Search embeddings
.AddStep("SemanticSearch", (data, ctx) =>
{
    var queryEmbedding = data.Get<float[]>("query_embedding");
    var topK = 3;
    
    var results = vectorStore
      .Select(item => new
        {
       item.Text,
     Score = CosineSimilarity(queryEmbedding, item.Embedding)
      })
  .OrderByDescending(x => x.Score)
        .Take(topK)
        .ToList();
    
    return Task.FromResult(data.Set("search_results", results));
})

static float CosineSimilarity(float[] a, float[] b)
{
    float dot = 0, magA = 0, magB = 0;
    for (int i = 0; i < a.Length; i++)
    {
     dot += a[i] * b[i];
        magA += a[i] * a[i];
    magB += b[i] * b[i];
  }
    return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
}
```

### Complete RAG Pipeline

```csharp
// Phase 1: Ingest documents
var ingestionPipeline = Workflow.Create("IngestDocuments")
    .AddNode(new FileReaderNode("ReadDoc", filePath))
    .AddNode(new ChunkTextNode(chunkConfig))
    .ForEach(
        itemsKey: "chunks",
 outputKey: "embedded_chunks",
        bodyBuilder: loop => loop
            .AddStep("PrepareChunk", (data, _) => {
           var chunk = data.Get<TextChunk>("__loop_item__");
       return Task.FromResult(data.Set("text", chunk.Text));
     })
            .AddNode(new EmbeddingNode("Embedder", embeddingConfig))
    )
    .AddStep("StoreInVectorDB", (data, _) => {
        var chunks = data.Get<List<WorkflowData>>("embedded_chunks");
        foreach (var chunk in chunks)
        {
          vectorStore.Add((
                chunk.GetString("text"),
      chunk.Get<float[]>("embedding")
            ));
        }
        return Task.FromResult(data);
    });

// Phase 2: Query with RAG
var queryPipeline = Workflow.Create("RAGQuery")
    // 1. Embed query
    .AddStep("PrepareQuery", (data, _) =>
        Task.FromResult(data.Set("text", data.GetString("question"))))
    .AddNode(new EmbeddingNode("QueryEmbedder", embeddingConfig))
    
    // 2. Search
    .AddStep("VectorSearch", (data, _) =>
    {
        var queryEmb = data.Get<float[]>("embedding");
      var results = vectorStore
     .Select(item => new { item.Text, Score = CosineSimilarity(queryEmb, item.Embedding) })
   .OrderByDescending(x => x.Score)
   .Take(5)
            .ToList();
 
        var context = string.Join("\n\n", results.Select(r => r.Text));
   return Task.FromResult(data.Set("retrieved_context", context));
    })
    
    // 3. Build RAG prompt
    .AddNode(new PromptBuilderNode("RAGPrompt",
        promptTemplate: """
      Answer based ONLY on this context:
     
       {{retrieved_context}}
            
            Question: {{question}}
 
            If the context doesn't contain the answer, say so.
         """,
        systemTemplate: "You are a precise Q&A assistant. Never hallucinate."))
    
    // 4. Generate answer
    .AddNode(new LlmNode("Answer", llmConfig with
    {
Temperature = Temperature.Deterministic
    }));
```

---

## 9. Multi-Turn Conversations

### Basic Conversation State

```csharp
var context = new WorkflowContext("ChatBot", logger);

var workflow = Workflow.Create("MultiTurnChat")
    .AddNode(new PromptBuilderNode("BuildPrompt",
 promptTemplate: "{{user_message}}"))
    
    .AddNode(new LlmNode("ChatLLM", llmConfig with
    {
   MaintainHistory = true  // Automatically maintains chat history
    }));

// Turn 1
var turn1 = await workflow.RunAsync(
    WorkflowData.From("user_message", "Hi, I'm Alice"),
    context
);

// Turn 2 (context preserved)
var turn2 = await workflow.RunAsync(
    WorkflowData.From("user_message", "What's my name?"),
    context
);
// LLM response: "Your name is Alice"
```

### Manual History Management

```csharp
var context = new WorkflowContext("ChatBot", logger);

.AddStep("SaveToHistory", (data, ctx) =>
{
    var userMsg = data.GetString("user_message");
    var botResponse = data.GetString("llm_response");
    
ctx.State.AppendMessage(ChatMessage.User(userMsg));
    ctx.State.AppendMessage(ChatMessage.Assistant(botResponse));
    
    return Task.FromResult(data);
})
```

### Session Management

```csharp
// Store session-specific data
context.State.Set("user_id", "u-12345");
context.State.Set("conversation_topic", "product_inquiry");
context.State.Set("session_start", DateTime.UtcNow);

// Retrieve in any node
.AddStep("PersonalizeResponse", (data, ctx) =>
{
    var userId = ctx.State.Get<string>("user_id");
    var topic = ctx.State.Get<string>("conversation_topic");
    
    // Use in workflow logic
    return Task.FromResult(data);
})
```

### Conversation Memory Limits

```csharp
// Limit history to last N messages
.AddStep("TruncateHistory", (data, ctx) =>
{
    var history = ctx.State.GetChatHistory();
    if (history.Count > 20)
    {
  // Keep only last 20 messages
 var recent = history.TakeLast(20).ToList();
        ctx.State.ClearChatHistory();
        foreach (var msg in recent)
        {
       ctx.State.AppendMessage(msg);
        }
    }
    return Task.FromResult(data);
})
```

---

## 10. Testing Workflows

### Unit Testing Nodes

```csharp
[Fact]
public async Task LlmNode_Should_Return_Response()
{
    // Arrange
    var mockHttpProvider = Substitute.For<IHttpClientProvider>();
    var mockClient = Substitute.For<HttpClient>();
    
    // Setup mock response
    mockHttpProvider.GetClient(Arg.Any<string>()).Returns(mockClient);
    
    var node = new LlmNode("TestLLM", llmConfig, mockHttpProvider);
    var input = WorkflowData.From("prompt", "Test prompt");
    var context = new WorkflowContext("Test", NullLogger.Instance);
  
    // Act
    var result = await node.ExecuteAsync(input, context);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.Has("llm_response").Should().BeTrue();
}
```

### Integration Testing Workflows

```csharp
[Fact]
public async Task Workflow_Should_Complete_Successfully()
{
    // Arrange
    var workflow = Workflow.Create("TestWorkflow")
        .AddNode(TestNode.Success("Node1", "key1", "value1"))
  .AddNode(TestNode.Success("Node2", "key2", "value2"));
    
    var input = WorkflowData.From("input", "test");
    
    // Act
  var result = await workflow.RunAsync(input);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Data.GetString("key1").Should().Be("value1");
    result.Data.GetString("key2").Should().Be("value2");
}
```

### Testing Error Handling

```csharp
[Fact]
public async Task Workflow_Should_Handle_Node_Failure()
{
    // Arrange
    var workflow = Workflow.Create("ErrorTest")
        .AddNode(TestNode.Success("Node1"))
        .AddNode(TestNode.Failure("FailNode", "Simulated error"))
        .AddNode(TestNode.Success("Node3"));  // Should not execute
    
    // Act
    var result = await workflow.RunAsync();
    
  // Assert
    result.IsFailure.Should().BeTrue();
    result.FailedNodeName.Should().Be("FailNode");
    result.ErrorMessage.Should().Contain("Simulated error");
    result.NodeResults.Should().HaveCount(2);  // Only Node1 and FailNode executed
}
```

### Mocking LLM Responses

```csharp
public class MockLlmNode : BaseNode
{
    private readonly string _mockResponse;
    
    public override string Name => "MockLLM";
    public override string Category => "AI";
    
    public MockLlmNode(string mockResponse)
    {
        _mockResponse = mockResponse;
    }
    
    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        return Task.FromResult(input.Set("llm_response", _mockResponse));
    }
}

// Use in tests
var workflow = Workflow.Create("Test")
    .AddNode(new PromptBuilderNode("Prompt", "{{question}}"))
    .AddNode(new MockLlmNode("This is a mock response"));

var result = await workflow.RunAsync(WorkflowData.From("question", "Test"));
result.Data.GetString("llm_response").Should().Be("This is a mock response");
```

---

## 11. Performance Optimization

### Use Parallel Execution

```csharp
//  Sequential (slow)
.AddNode(new SentimentNode())
.AddNode(new KeywordNode())
.AddNode(new CategoryNode())

//  Parallel (3x faster)
.Parallel(
    new SentimentNode(),
    new KeywordNode(),
    new CategoryNode()
)
```

### Batch Operations

```csharp
//  Individual API calls in loop (slow)
.ForEach(
    itemsKey: "items",
    outputKey: "results",
    bodyBuilder: loop => loop
.AddNode(new HttpRequestNode("CallAPI", apiConfig))
)

//  Batch API call (single request)
.AddStep("BatchProcess", async (data, ctx) =>
{
  var items = data.Get<List<object>>("items");
    var batchResult = await CallBatchAPI(items);  // Single API call
    return data.Set("results", batchResult);
})
```

### Connection Pooling

```csharp
// Use PooledHttpClientProvider for better connection management
services.AddSingleton<IHttpClientProvider, PooledHttpClientProvider>();

var node = new HttpRequestNode("API", config, pooledProvider);
```

### Token Optimization

```csharp
// Reduce token usage with smaller models when appropriate
var llmConfig = modelComplexity switch
{
    "simple" => LlmConfig.OpenAI(apiKey, "gpt-4o-mini"),   // Cheaper, faster
    "complex" => LlmConfig.OpenAI(apiKey, "gpt-4o"),       // More capable
    _ => LlmConfig.OpenAI(apiKey, "gpt-4o")
};

// Limit response length
llmConfig = llmConfig with
{
    MaxTokens = TokenCount.FromValue(500)  // Don't pay for more than you need
};
```

### Caching Strategies

```csharp
var cache = new Dictionary<string, string>();

.AddStep("CachedLLMCall", async (data, ctx) =>
{
    var prompt = data.GetString("prompt");
    var cacheKey = HashPrompt(prompt);
    
    if (cache.TryGetValue(cacheKey, out var cached))
    {
      ctx.Logger.LogInformation("Cache hit");
  return data.Set("llm_response", cached);
    }
    
    // Call LLM
    var llmNode = new LlmNode("LLM", llmConfig);
    var result = await llmNode.ExecuteAsync(data, ctx);
    
    if (result.IsSuccess)
    {
        var response = result.Data.GetString("llm_response");
        cache[cacheKey] = response;
    }
    
 return result.Data;
})
```

---

## 12. Deployment

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHttpClientProvider, PooledHttpClientProvider>();
builder.Services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();

var app = builder.Build();

app.MapPost("/api/chat", async (ChatRequest request, IWorkflowService workflowService) =>
{
    var result = await workflowService.ExecuteChatWorkflowAsync(request.Message);
    return Results.Ok(new { response = result.Data.GetString("llm_response") });
});

// WorkflowService.cs
public class WorkflowService : IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly IHttpClientProvider _httpProvider;
    private readonly IConfiguration _config;
    
    public WorkflowService(
        ILogger<WorkflowService> logger,
    IHttpClientProvider httpProvider,
        IConfiguration config)
    {
   _logger = logger;
        _httpProvider = httpProvider;
 _config = config;
    }
    
    public async Task<WorkflowResult> ExecuteChatWorkflowAsync(string userMessage)
    {
      var apiKey = _config["OpenAI:ApiKey"];
    var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o");
      
        var workflow = Workflow.Create("ChatAPI")
        .UseLogger(_logger)
 .AddNode(new PromptBuilderNode("Prompt", "{{message}}"))
          .AddNode(new LlmNode("Chat", llmConfig, _httpProvider));
        
        var input = WorkflowData.From("message", userMessage);
   return await workflow.RunAsync(input);
    }
}
```

### Azure Functions

```csharp
public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    
    public ChatFunction(ILogger<ChatFunction> logger)
    {
        _logger = logger;
    }
    
[Function("Chat")]
    public async Task<HttpResponseData> Run(
   [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody);
    
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o");
      
 var workflow = Workflow.Create("AzureFunctionChat")
            .UseLogger(_logger)
            .AddNode(new PromptBuilderNode("Prompt", "{{message}}"))
        .AddNode(new LlmNode("Chat", llmConfig));
        
        var input = WorkflowData.From("message", chatRequest.Message);
var result = await workflow.RunAsync(input);
    
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
    {
            response = result.Data.GetString("llm_response")
   });
        
 return response;
    }
}
```

### Background Jobs

```csharp
// Using Hangfire
public class WorkflowBackgroundService
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessDocumentsAsync(string filePath)
    {
   var workflow = Workflow.Create("DocumentProcessor")
        .AddNode(new FileReaderNode("Read", filePath))
  .AddNode(new ChunkTextNode(chunkConfig))
            .ForEach(
   itemsKey: "chunks",
      outputKey: "embeddings",
                bodyBuilder: loop => loop
          .AddNode(new EmbeddingNode("Embedder", embeddingConfig))
            )
            .AddStep("SaveToDatabase", async (data, ctx) =>
      {
   await SaveEmbeddingsAsync(data.Get<List<float[]>>("embeddings"));
        return data;
        });
   
      await workflow.RunAsync();
    }
}

// Schedule job
BackgroundJob.Enqueue<WorkflowBackgroundService>(x => 
    x.ProcessDocumentsAsync("path/to/document.txt"));
```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyWorkflowApp/MyWorkflowApp.csproj", "MyWorkflowApp/"]
RUN dotnet restore "MyWorkflowApp/MyWorkflowApp.csproj"
COPY . .
WORKDIR "/src/MyWorkflowApp"
RUN dotnet build "MyWorkflowApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyWorkflowApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyWorkflowApp.dll"]
```

### Environment Configuration

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Twf.Flow": "Debug"
    }
  },
  "OpenAI": {
    "ApiKey": "",  // Set via environment variable or user secrets
    "Model": "gpt-4o",
    "MaxTokens": 2000
  },
  "Workflows": {
    "DefaultTimeout": "00:00:30",
    "MaxRetries": 3
  }
}

// Use in code
var config = builder.Configuration.GetSection("OpenAI");
var llmConfig = LlmConfig.OpenAI(
    config["ApiKey"],
    config["Model"]
) with
{
    MaxTokens = TokenCount.FromValue(config.GetValue<int>("MaxTokens"))
};
```

---

## Summary

This guide covered:

 **Getting Started**: Installation and basic setup  
 **Workflows**: Sequential, parallel, conditional, looping  
 **LLMs**: All major providers, streaming, temperature control  
 **Data**: Transformations, validation, type conversions  
 **Control Flow**: Branching, loops, try-catch  
 **Error Handling**: Retry, fallbacks, workflow-level handlers  
 **APIs**: HTTP requests, rate limiting, error handling  
 **RAG**: Chunking, embeddings, vector search  
 **Conversations**: Multi-turn, history management  
 **Testing**: Unit tests, mocks, integration tests  
 **Performance**: Parallelism, batching, caching  
 **Deployment**: ASP.NET Core, Azure Functions, Docker  

For more examples, see:
- `source/console/examples/`  Complete workflow examples
- `source/console/concepts/`  Framework concept demonstrations
- `tests/`  Comprehensive test suite

**Need help?** Check the [GitHub repository](https://github.com/techwayfit/Twf_AI_Framework) for issues and discussions.
