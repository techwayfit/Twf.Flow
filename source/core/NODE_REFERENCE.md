# Twf.Flow � Complete Node Reference

This document provides detailed reference documentation for all built-in nodes in Twf.Flow, including parameters, inputs, outputs, and usage examples.

---

## Table of Contents

### AI Nodes
- [LlmNode](#llmnode)
- [PromptBuilderNode](#promptbuildernode)
- [OutputParserNode](#outputparsernode)
- [EmbeddingNode](#embeddingnode)

### Data Nodes
- [TransformNode](#transformnode)
- [DataMapperNode](#datamappernode)
- [FilterNode](#filternode)
- [ChunkTextNode](#chunktextnode)
- [MemoryNode](#memorynode)
- [SetVariableNode](#setvariablenode)
- [JsonParseNode](#jsonparsenode)
- [JsonStringifyNode](#jsonstringifynode)
- [MathOperationNode](#mathoperationnode)
- [CreateListNode](#createlistnode)
- [AddListItemNode](#addlistitemnode)
- [RemoveListItemNode](#removelistitemnode)
- [MergeListsNode](#mergelistsnode)
- [AppendStringNode](#appendstringnode)

### Control Nodes
- [ConditionNode](#conditionnode)
- [BranchNode](#branchnode)
- [TryCatchNode](#trycatchnode)
- [DelayNode](#delaynode)
- [LogNode](#lognode)
- [MergeNode](#mergenode)
- [ErrorRouteNode](#errorroutenode)
- [LoopNode](#loopnode)

### IO Nodes
- [HttpRequestNode](#httprequestnode)
- [FileReaderNode](#filereadernode)
- [FileWriterNode](#filewriternode)
- [CsvReadNode](#csvreadnode)
- [CsvWriteNode](#csvwritenode)
- [GoogleSearchNode](#googlesearchnode)

### Base Classes
- [BaseNode](#basenode)
- [SimpleTransformNode](#simpletransformnode)
- [LambdaNode](#lambdanode)

---

## AI Nodes

### LlmNode

**Category:** AI  
**Purpose:** Call any OpenAI-compatible LLM API (OpenAI, Anthropic, Azure OpenAI, Ollama, custom providers)

#### Constructor

```csharp
public LlmNode(
    string name,
    LlmConfig config,
    IHttpClientProvider? httpProvider = null,
    ISecretProvider? secretProvider = null,
    IPromptSanitizer? promptSanitizer = null)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `config` | LlmConfig | Yes | LLM configuration |
| `httpProvider` | IHttpClientProvider | No | HTTP client provider (default: shared client) |
| `secretProvider` | ISecretProvider | No | Secret resolver (default: environment variables) |
| `promptSanitizer` | IPromptSanitizer | No | Prompt sanitization (default: basic sanitization) |

#### LlmConfig Properties

```csharp
public record LlmConfig
{
    public LlmProvider Provider { get; init; }
    public string Model { get; init; }
    public string ApiKey { get; init; }
    public string ApiEndpoint { get; init; }
    public string? DefaultSystemPrompt { get; init; }
    public Temperature Temperature { get; init; }
  public TokenCount MaxTokens { get; init; }
    public bool MaintainHistory { get; init; }
    public bool Stream { get; init; }
    public Action<string>? OnChunk { get; init; }
    public bool SanitizePrompts { get; init; }
    public PromptSanitizationOptions? SanitizationOptions { get; init; }
}
```

#### Factory Methods

```csharp
// OpenAI
LlmConfig.OpenAI(apiKey, model)

// Anthropic
LlmConfig.Anthropic(apiKey, model)

// Azure OpenAI
LlmConfig.AzureOpenAI(apiKey, model, endpoint)

// Ollama (local)
LlmConfig.Ollama(model, host = "http://localhost:11434")

// Custom OpenAI-compatible
LlmConfig.Custom(model, apiKey, endpoint)
```

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `prompt` | string | Yes* | User message to send |
| `messages` | List<(string Role, string Content)> | Yes* | Full message array (overrides `prompt`) |
| `system_prompt` | string | No | System instruction (overrides config) |

*Either `prompt` or `messages` must be provided

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `llm_response` | string | Model's text response |
| `llm_model` | string | Model name used |
| `prompt_tokens` | int | Tokens consumed by prompt |
| `completion_tokens` | int | Tokens in completion |

#### Examples

**Basic usage:**
```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o");
var llmNode = new LlmNode("ChatGPT", llmConfig);

var input = WorkflowData.From("prompt", "What is 2+2?");
var result = await llmNode.ExecuteAsync(input, context);
var answer = result.Data.GetString("llm_response");
```

**With conversation history:**
```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o") with
{
MaintainHistory = true
};

// Turn 1
await llmNode.ExecuteAsync(
    WorkflowData.From("prompt", "My name is Alice"),
    context
);

// Turn 2 (remembers Alice)
await llmNode.ExecuteAsync(
    WorkflowData.From("prompt", "What's my name?"),
    context
);
```

**Streaming:**
```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o") with
{
    Stream = true,
    OnChunk = chunk => Console.Write(chunk)  // Real-time output
};
```

**Temperature control:**
```csharp
// Factual/deterministic
var config = llmConfig with { Temperature = Temperature.Deterministic };

// Balanced
var config = llmConfig with { Temperature = Temperature.Balanced };

// Creative
var config = llmConfig with { Temperature = Temperature.Creative };

// Custom
var config = llmConfig with { Temperature = Temperature.FromValue(0.9f) };
```

**Prompt sanitization:**
```csharp
var llmConfig = LlmConfig.OpenAI(apiKey, "gpt-4o") with
{
    SanitizePrompts = true,
    SanitizationOptions = new()
    {
        Mode = PromptSanitizationMode.Strict,
        MaxLength = 10000,
        BlockSqlInjection = true,
        BlockXss = true,
   BlockPii = true
    }
};
```

---

### PromptBuilderNode

**Category:** AI  
**Purpose:** Build dynamic prompts from templates using `{{variable}}` substitution

#### Constructor

```csharp
public PromptBuilderNode(
    string name,
    string promptTemplate,
    string? systemTemplate = null,
    Dictionary<string, object?>? staticVariables = null)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `promptTemplate` | string | Yes | Prompt template with `{{placeholders}}` |
| `systemTemplate` | string | No | System prompt template |
| `staticVariables` | Dictionary | No | Static variable overrides |

#### Template Syntax

```
{{variable_name}}   - Replaced with value from WorkflowData or static variables
{{MISSING:variable_name}}  - Output when variable not found
```

#### Data In

All keys referenced in templates via `{{key}}` are read from WorkflowData.

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `prompt` | string | Rendered prompt |
| `system_prompt` | string | Rendered system prompt (if systemTemplate provided) |

#### Examples

**Basic template:**
```csharp
var node = new PromptBuilderNode(
    name: "BuildPrompt",
    promptTemplate: "Translate '{{text}}' to {{language}}",
    systemTemplate: "You are a professional translator."
);

var input = WorkflowData.From("text", "Hello")
    .Set("language", "Spanish");

var result = await node.ExecuteAsync(input, context);
// prompt: "Translate 'Hello' to Spanish"
// system_prompt: "You are a professional translator."
```

**Multi-line template:**
```csharp
var template = """
    You are analyzing customer feedback.
    
    Customer: {{customer_name}}
    Message: "{{message}}"
    Previous interactions: {{interaction_count}}
    
    Classify sentiment and extract key concerns.
    Return JSON: {"sentiment": "...", "concerns": [...]}
    """;

var node = new PromptBuilderNode("Analyzer", template);
```

**Static variables (fallbacks):**
```csharp
var node = new PromptBuilderNode(
    name: "Prompt",
    promptTemplate: "Hello {{name}}, welcome to {{company}}!",
    staticVariables: new()
    {
     { "company", "TechWay Inc." }  // Default if not in WorkflowData
    }
);
```

---

### OutputParserNode

**Category:** AI  
**Purpose:** Extract structured JSON from LLM responses, handling markdown code fences

#### Constructor

```csharp
public OutputParserNode(
    string name,
    Dictionary<string, string>? fieldMapping = null,
    bool strict = true)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `fieldMapping` | Dictionary<string, string> | No | Maps JSON keys to WorkflowData keys |
| `strict` | bool | No | If true, throws on parse failure; if false, passes data through |

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `llm_response` | string | Yes | Raw LLM text output |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `parsed_output` | object | Full parsed JSON object |
| *(mapped keys)* | various | Individual fields per fieldMapping |

#### Examples

**Parse all JSON fields:**
```csharp
var node = new OutputParserNode("Parser");

// LLM response: {"sentiment": "positive", "score": 0.95, "summary": "..."}
var result = await node.ExecuteAsync(input, context);

var parsed = result.Data.Get<Dictionary<string, object>>("parsed_output");
// All fields available in parsed_output
```

**Map specific fields:**
```csharp
var node = new OutputParserNode("Parser", new Dictionary<string, string>
{
  ["sentiment"] = "user_sentiment",  // JSON key ? WorkflowData key
    ["score"] = "confidence",
    ["concerns"] = "extracted_concerns"
});

// Output:
// user_sentiment: "positive"
// confidence: 0.95
// extracted_concerns: [...]
```

**Factory methods:**
```csharp
// Map multiple fields
OutputParserNode.WithMapping(
    "Parser",
    ("json_key1", "data_key1"),
    ("json_key2", "data_key2")
)

// Extract single field
OutputParserNode.ExtractField("Parser", "sentiment")
```

**Handles markdown code fences:**
```csharp
// LLM response:
// ```json
// {"status": "success"}
// ```

var node = new OutputParserNode("Parser");
// Automatically strips ```json and ``` before parsing
```

---

### EmbeddingNode

**Category:** AI  
**Purpose:** Generate vector embeddings for semantic search and RAG

#### Constructor

```csharp
public EmbeddingNode(
    string name,
    EmbeddingConfig config)
```

#### EmbeddingConfig Properties

```csharp
public class EmbeddingConfig
{
    public string Model { get; set; }
    public string ApiKey { get; set; }
    public string ApiUrl { get; set; }
}
```

#### Factory Methods

```csharp
// OpenAI
EmbeddingConfig.OpenAI(apiKey, model = "text-embedding-3-small")

// Azure OpenAI
EmbeddingConfig.AzureOpenAI(apiKey, model, endpoint)
```

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `text` | string | Yes* | Single string to embed |
| `texts` | List<string> | Yes* | Multiple strings to embed |

*Provide either `text` or `texts`

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `embedding` | float[] | Single embedding vector |
| `embeddings` | List<float[]> | Multiple embedding vectors |
| `embedding_model` | string | Model name used |

#### Examples

**Single text:**
```csharp
var config = EmbeddingConfig.OpenAI(apiKey);
var node = new EmbeddingNode("Embedder", config);

var input = WorkflowData.From("text", "This is a sample text");
var result = await node.ExecuteAsync(input, context);

var embedding = result.Data.Get<float[]>("embedding");
// float[1536] for text-embedding-3-small
```

**Batch embeddings:**
```csharp
var texts = new List<string> { "Text 1", "Text 2", "Text 3" };
var input = WorkflowData.From("texts", texts);
var result = await node.ExecuteAsync(input, context);

var embeddings = result.Data.Get<List<float[]>>("embeddings");
// List of 3 float[] vectors
```

**Cosine similarity helper:**
```csharp
public static float CosineSimilarity(float[] a, float[] b)
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

---

## Data Nodes

### TransformNode

**Category:** Data  
**Purpose:** Apply custom transformations to WorkflowData

#### Constructor

```csharp
public TransformNode(
    string name,
    Func<WorkflowData, WorkflowData> transform)
```

#### Factory Methods

```csharp
// Rename a key
TransformNode.Rename(fromKey, toKey)

// Select nested value
TransformNode.SelectKey(sourcePath, targetKey)

// Concatenate strings
TransformNode.ConcatStrings(outputKey, separator, params string[] inputKeys)

// Custom transform
new TransformNode("MyTransform", data =>
{
    // Custom logic
    return data.Set("result", transformedValue);
})
```

#### Examples

```csharp
// Rename
.AddNode(TransformNode.Rename("old_name", "new_name"))

// Concatenate
.AddNode(TransformNode.ConcatStrings("full_name", " ", "first_name", "last_name"))

// Custom
.AddNode(new TransformNode("Uppercase", data =>
{
    var text = data.GetString("input");
    return data.Set("output", text?.ToUpperInvariant());
}))
```

---

### DataMapperNode

**Category:** Data  
**Purpose:** Map values from source paths to target keys with default fallbacks

#### Constructor

```csharp
public DataMapperNode(
    string name,
    Dictionary<string, string> mappings,
    Dictionary<string, object?>? defaultValues = null,
    bool throwOnMissing = false,
    bool removeUnmapped = false)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `mappings` | Dictionary | Yes | `targetKey: sourcePath` pairs |
| `defaultValues` | Dictionary | No | Fallback values for missing sources |
| `throwOnMissing` | bool | No | Error if source not found |
| `removeUnmapped` | bool | No | Strip unmapped keys |

#### Source Path Syntax

```
simple_key    - Top-level key
nested.path.to.value          - Nested object access
array.0.field   - Array index access
http_response.data.customer.id - Deep nested path
```

#### Examples

```csharp
// Map nested HTTP response
var mapper = new DataMapperNode("Extract", new Dictionary<string, string>
{
    { "customer_id", "http_response.data.customer.id" },
  { "email", "http_response.data.customer.email" },
    { "name", "http_response.data.customer.name" }
});

// With defaults
var mapper = new DataMapperNode("MapWithDefaults",
    mappings: new()
    {
     { "user_id", "profile.id" },
 { "tier", "profile.subscription.tier" }
    },
    defaultValues: new()
    {
   { "tier", "free" }  // Default if path not found
    }
);

// Strict mode
var mapper = new DataMapperNode("StrictMap",
    mappings: new() { { "required_field", "source.path" } },
    throwOnMissing: true  // Throws if source.path doesn't exist
);
```

---

### FilterNode

**Category:** Data  
**Purpose:** Validate data against conditions

#### Constructor

```csharp
public FilterNode(
    string name,
    bool strict = true)
```

#### Methods

```csharp
public FilterNode RequireNonEmpty(string key)
public FilterNode MaxLength(string key, int maxLength)
public FilterNode MinLength(string key, int minLength)
public FilterNode AddCondition(string key, Func<object?, bool> predicate, string? errorMessage = null)
```

#### Examples

```csharp
// Basic validation
var filter = new FilterNode("Validate")
    .RequireNonEmpty("email")
    .RequireNonEmpty("password")
    .MinLength("password", 8)
    .MaxLength("message", 2000);

// Custom conditions
var filter = new FilterNode("ValidateAge")
    .AddCondition("age", value =>
    {
    var age = Convert.ToInt32(value);
        return age >= 18 && age <= 120;
    }, "Age must be between 18 and 120");

// Non-strict mode
var filter = new FilterNode("SoftValidate", strict: false);
// Sets "is_valid" flag instead of throwing
```

---

### ChunkTextNode

**Category:** Data  
**Purpose:** Split large text into overlapping chunks for RAG pipelines

#### Constructor

```csharp
public ChunkTextNode(ChunkConfig config)
```

#### ChunkConfig Properties

```csharp
public class ChunkConfig
{
    public ChunkSize ChunkSize { get; set; }
    public ChunkOverlap Overlap { get; set; }
    public ChunkStrategy Strategy { get; set; }
}

public enum ChunkStrategy
{
    Character,  // Split by character count
    Word,   // Split by word count
    Sentence    // Split by sentence boundaries (best for embeddings)
}
```

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `text` | string | Yes | Source text to chunk |
| `source` | string | No | Source label (included in chunk metadata) |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `chunks` | List<TextChunk> | List of text chunks |
| `chunk_count` | int | Number of chunks produced |

#### TextChunk Properties

```csharp
public class TextChunk
{
    public string Text { get; set; }
    public string Source { get; set; }
    public int Index { get; set; }
    public int StartPos { get; set; }
    public int EndPos { get; set; }
}
```

#### Examples

```csharp
// Sentence-based chunking (recommended for RAG)
var config = new ChunkConfig
{
    ChunkSize = ChunkSize.FromValue(500),   // ~500 characters
    Overlap = ChunkOverlap.FromValue(100),     // 100 char overlap
    Strategy = ChunkStrategy.Sentence
};
var node = new ChunkTextNode(config);

// Word-based chunking
var config = new ChunkConfig
{
    ChunkSize = ChunkSize.FromValue(150),      // 150 words
    Overlap = ChunkOverlap.FromValue(30),      // 30 word overlap
    Strategy = ChunkStrategy.Word
};

// Process chunks in a loop
.AddNode(new ChunkTextNode(chunkConfig))
.ForEach(
    itemsKey: "chunks",
    outputKey: "embeddings",
    bodyBuilder: loop => loop
        .AddStep("ExtractText", (data, _) =>
        {
            var chunk = data.Get<TextChunk>("__loop_item__");
       return Task.FromResult(data.Set("text", chunk.Text));
        })
    .AddNode(new EmbeddingNode("Embedder", embeddingConfig))
);
```

---

### MemoryNode

**Category:** Data  
**Purpose:** Read/write workflow global state (persists across nodes)

#### Factory Methods

```csharp
// Write keys to global memory
MemoryNode.Write(params string[] keys)

// Read keys from global memory
MemoryNode.Read(params string[] keys)
```

#### Examples

```csharp
// Write to memory
.AddNode(MemoryNode.Write("user_id", "session_id", "preferences"))

// Read from memory
.AddNode(MemoryNode.Read("user_id", "session_id"))

// Use case: Session persistence
var context = new WorkflowContext("App", logger);

// First request
await workflow.RunAsync(
    WorkflowData.From("user_id", "u-123")
        .Set("preferences", userPrefs),
    context
);

// Later request (user_id still in global memory)
await workflow.RunAsync(
    WorkflowData.From("action", "update"),
    context
);
```

---

### SetVariableNode

**Category:** Data  
**Purpose:** Set workflow-level variables

#### Constructor

```csharp
public SetVariableNode(
    string name,
    Dictionary<string, object?> variables)
```

#### Examples

```csharp
.AddNode(new SetVariableNode("SetConfig", new Dictionary<string, object?>
{
 { "api_version", "v2" },
    { "timeout_seconds", 30 },
    { "retry_enabled", true }
}))
```

---

### JsonParseNode

**Category:** Data  
**Purpose:** Parse a JSON string stored in WorkflowData into a structured .NET object

#### Constructor

```csharp
public JsonParseNode(
    string name,
    string inputKey,
    string outputKey = "parsed_json",
    bool strict = false)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `inputKey` | string | Yes | WorkflowData key holding the JSON string |
| `outputKey` | string | No | Key to write the parsed object to (default: `parsed_json`) |
| `strict` | bool | No | Throw on parse failure instead of setting success flag |

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `{inputKey}` | string | Yes | JSON string to parse |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | object | Deserialized JSON value |
| `json_parse_success` | bool | Whether parsing succeeded |

#### Examples

```csharp
// Parse an LLM response that was already stored as a JSON string
var node = new JsonParseNode("ParseConfig", inputKey: "config_json");

// In strict mode — throws instead of setting json_parse_success=false
var node = new JsonParseNode("ParseStrict", inputKey: "data", strict: true);

// Combined with LLM output
.AddNode(new LlmNode("LLM", llmConfig))
.AddNode(new JsonParseNode("Parse", inputKey: LlmNode.OutputResponse, outputKey: "structured"))
```

---

### JsonStringifyNode

**Category:** Data  
**Purpose:** Serialize any WorkflowData value to a JSON string

#### Constructor

```csharp
public JsonStringifyNode(
    string name,
    string inputKey,
    string outputKey = "json_string",
    bool indented = false)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `inputKey` | string | Yes | WorkflowData key to serialize |
| `outputKey` | string | No | Key to write the JSON string to (default: `json_string`) |
| `indented` | bool | No | Format with indentation for readability |

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `{inputKey}` | object | Yes | Value to serialize |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | string | JSON string representation |

#### Examples

```csharp
// Compact output (default)
var node = new JsonStringifyNode("Stringify", inputKey: "my_data");

// Pretty-printed
var node = new JsonStringifyNode("StringifyPretty", inputKey: "my_data", indented: true);

// Serialize search results before sending to an API
.AddNode(new GoogleSearchNode("Search", searchConfig))
.AddNode(new JsonStringifyNode("ToJson", inputKey: "search_results", outputKey: "payload"))
.AddNode(new HttpRequestNode("Post", postConfig))
```

---

### MathOperationNode

**Category:** Data  
**Purpose:** Perform arithmetic operations on numeric WorkflowData values

#### Constructor

```csharp
public MathOperationNode(
    string name,
    string operation,
    string outputKey,
    string? inputKeyA = null,
    double valueA = 0,
    string? inputKeyB = null,
    double? valueB = null)
```

#### Supported Operations

| Operation | Expression | Operands |
|-----------|-----------|---------|
| `add` | A + B | A and B |
| `subtract` | A − B | A and B |
| `multiply` | A × B | A and B |
| `divide` | A ÷ B | A and B |
| `modulo` | A % B | A and B |
| `power` | A ^ B | A and B |
| `min` | min(A, B) | A and B |
| `max` | max(A, B) | A and B |
| `abs` | \|A\| | A only |
| `sqrt` | √A | A only |
| `round` | round(A) | A only |
| `floor` | floor(A) | A only |
| `ceil` | ceil(A) | A only |
| `negate` | −A | A only |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | double | Result of the operation |

#### Examples

```csharp
// Add two WorkflowData values
new MathOperationNode("Sum", "add", "total", inputKeyA: "count_a", inputKeyB: "count_b")

// Increment a counter by a literal
new MathOperationNode("Increment", "add", "counter", inputKeyA: "counter", valueB: 1)

// Absolute value
new MathOperationNode("Abs", "abs", "magnitude", inputKeyA: "delta")

// Divide with literals
new MathOperationNode("Half", "divide", "half_count", inputKeyA: "total", valueB: 2)
```

---

### CreateListNode

**Category:** Data  
**Purpose:** Create a new list and store it in WorkflowData, optionally pre-populated

#### Constructor

```csharp
public CreateListNode(
    string name,
    string listKey,
    List<object?>? initial = null)
```

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{listKey}` | List&lt;object?&gt; | The newly created list |
| `list_count` | int | Number of items in the created list |

#### Examples

```csharp
// Empty list
new CreateListNode("Init", listKey: "results")

// Pre-populated list
new CreateListNode("InitTags", listKey: "tags",
    initial: new() { "ai", "workflow", "dotnet" })

// Use with AddListItemNode to build lists incrementally
.AddNode(new CreateListNode("InitResults", listKey: "results"))
.ForEach(itemsKey: "documents", outputKey: "processed", bodyBuilder: loop => loop
    .AddNode(new LlmNode("Summarize", llmConfig))
    .AddNode(new AddListItemNode("Collect", listKey: "results", itemKey: LlmNode.OutputResponse))
)
```

---

### AddListItemNode

**Category:** Data  
**Purpose:** Append or prepend an item to an existing list in WorkflowData

#### Constructor

```csharp
public AddListItemNode(
    string name,
    string listKey,
    string? itemKey = null,
    string? itemValue = null,
    bool prepend = false)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `listKey` | string | Yes | WorkflowData key of the list to modify (created if absent) |
| `itemKey` | string | No | Read item from this WorkflowData key (takes priority over `itemValue`) |
| `itemValue` | string | No | Literal value to add |
| `prepend` | bool | No | Insert at start rather than end |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{listKey}` | List&lt;object?&gt; | Updated list |
| `list_count` | int | New length of the list |

#### Examples

```csharp
// Append a literal
new AddListItemNode("AddLabel", listKey: "tags", itemValue: "reviewed")

// Append from a WorkflowData key
new AddListItemNode("CollectReply", listKey: "replies", itemKey: "llm_response")

// Prepend (insert at beginning)
new AddListItemNode("Prepend", listKey: "messages", itemValue: "SYSTEM", prepend: true)
```

---

### RemoveListItemNode

**Category:** Data  
**Purpose:** Remove an item from a list by index or by value

#### Constructor

```csharp
public RemoveListItemNode(
    string name,
    string listKey,
    int? removeIndex = null,
    string? removeValue = null)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `listKey` | string | Yes | WorkflowData key of the list to modify |
| `removeIndex` | int? | No | 0-based index to remove; negative values count from end (`-1` = last item) |
| `removeValue` | string? | No | Remove first item whose string representation matches this value |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{listKey}` | List&lt;object?&gt; | Updated list (item removed) |
| `list_count` | int | New length |
| `removed_item` | object | The item that was removed (null if not found) |

#### Examples

```csharp
// Remove by 0-based index
new RemoveListItemNode("RemoveFirst", listKey: "items", removeIndex: 0)

// Remove last item
new RemoveListItemNode("RemoveLast", listKey: "items", removeIndex: -1)

// Remove by value
new RemoveListItemNode("RemoveLabel", listKey: "tags", removeValue: "draft")
```

---

### MergeListsNode

**Category:** Data  
**Purpose:** Concatenate two lists from WorkflowData into a single output list

#### Constructor

```csharp
public MergeListsNode(
    string name,
    string listKeyA,
    string listKeyB,
    string outputKey,
    bool deduplicate = false)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `listKeyA` | string | Yes | First list key |
| `listKeyB` | string | Yes | Second list key |
| `outputKey` | string | Yes | Key to write the merged list to |
| `deduplicate` | bool | No | Remove duplicate items (by string representation) |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | List&lt;object?&gt; | Merged list |
| `list_count` | int | Total item count |

#### Examples

```csharp
// Concatenate two result lists
new MergeListsNode("Combine", listKeyA: "results_a", listKeyB: "results_b", outputKey: "all_results")

// Merge and deduplicate tags
new MergeListsNode("MergeTags", listKeyA: "tags_old", listKeyB: "tags_new",
    outputKey: "tags", deduplicate: true)
```

---

### AppendStringNode

**Category:** Data  
**Purpose:** Concatenate a value onto an existing string in WorkflowData

#### Constructor

```csharp
public AppendStringNode(
    string name,
    string targetKey,
    string? appendKey = null,
    string? appendValue = null,
    string separator = "")
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `targetKey` | string | Yes | Key of the string to append to (created empty if missing) |
| `appendKey` | string | No | WorkflowData key whose value is appended (takes priority over `appendValue`) |
| `appendValue` | string | No | Literal string to append. Supports `{{variable}}` interpolation |
| `separator` | string | No | Inserted between the base string and the appended value (default: empty) |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{targetKey}` | string | Resulting concatenated string |

#### Examples

```csharp
// Append a literal suffix
new AppendStringNode("AddSuffix", targetKey: "message", appendValue: " (reviewed)")

// Append from another key with a separator
new AppendStringNode("Join", targetKey: "full_name",
    appendKey: "last_name", separator: " ")

// Build up a log string in a loop
.AddNode(new AppendStringNode("Log", targetKey: "log",
    appendKey: "step_result", separator: "\n"))
```

---

## Control Nodes

### ConditionNode

**Category:** Control  
**Purpose:** Evaluate boolean predicates and write results to WorkflowData

#### Constructor

```csharp
public ConditionNode(
    string name,
    params (string OutputKey, Func<WorkflowData, bool> Condition)[] conditions)
```

#### Factory Methods

```csharp
// Check if key exists
ConditionNode.HasKey(outputKey, dataKey)

// String equality
ConditionNode.StringEquals(outputKey, dataKey, expectedValue, caseSensitive = false)

// Length check
ConditionNode.LengthExceeds(outputKey, dataKey, maxLength)

// Numeric comparison
ConditionNode.NumericGreaterThan(outputKey, dataKey, threshold)
```

#### Data Out

Each condition creates a boolean key in WorkflowData.

#### Examples

```csharp
// Multiple conditions
var node = new ConditionNode("Checks",
  ("is_premium", data => data.GetString("tier") == "premium"),
    ("is_active", data => data.Get<bool>("active") == true),
    ("has_credits", data => data.Get<int>("credits") > 0)
);

// Factory method
.AddNode(ConditionNode.StringEquals("CheckLang", "language", "en"))

// Use with branching
.AddNode(new ConditionNode("ScoreCheck",
  ("is_high_score", data => data.Get<int>("score") > 80)))
.Branch(
    condition: data => data.Get<bool>("is_high_score"),
    trueBranch: high => high.AddNode(new CelebrationNode()),
    falseBranch: low => low.AddNode(new EncouragementNode())
)
```

---

### BranchNode

**Category:** Control  
**Purpose:** Switch/case routing based on value matching

#### Constructor

```csharp
public BranchNode(
    string name,
    string valueKey,
    params KeyValuePair<string, Workflow>[] cases)
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `valueKey` | string | Yes | WorkflowData key to evaluate |
| `cases` | KeyValuePair[] | Yes | Up to 3 cases + default |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `branch_selected_port` | string | Which port was taken (`case1`/`case2`/`case3`/`default`) |
| `branch_input_value` | string | The value that was tested |
| `branch_selected_value` | string | The matched case value |
| `branch_case1` | bool | True if case1 matched |
| `branch_case2` | bool | True if case2 matched |
| `branch_case3` | bool | True if case3 matched |
| `branch_default` | bool | True if no case matched |

#### Examples

```csharp
// Route by intent
var router = new BranchNode("IntentRouter", "intent",
    new("greeting", greetingWorkflow),
    new("complaint", complaintWorkflow),
    new("billing", billingWorkflow)
);

.AddNode(router)
// Execution continues in the matched sub-workflow

// With explicit case setup
var caseGreeting = Workflow.Create("Greeting")
    .AddNode(new PromptBuilderNode("GreetPrompt", "Hello! How can I help?"))
    .AddNode(new LlmNode("GreetLLM", llmConfig));

var caseComplaint = Workflow.Create("Complaint")
    .AddNode(new PromptBuilderNode("ComplaintPrompt", "I understand your frustration..."))
    .AddNode(new LlmNode("ComplaintLLM", llmConfig));

.AddNode(new BranchNode("Router", "customer_intent",
    new("greeting", caseGreeting),
    new("complaint", caseComplaint)
))
```

---

### TryCatchNode

**Category:** Control  
**Purpose:** Exception handling with try/catch workflow composition

#### Constructor

```csharp
public TryCatchNode(
    string name,
    Action<Workflow> tryBuilder,
Action<Workflow> catchBuilder)
```

#### Data Out (on catch)

| Key | Type | Description |
|-----|------|-------------|
| `caught_error_message` | string | Exception message |
| `caught_failed_node` | string | Name of node that failed |
| `caught_exception_type` | string | Exception type name |

#### Always Written

| Key | Type | Description |
|-----|------|-------------|
| `try_catch_route` | string | `"success"` or `"catch"` |
| `try_success` | bool | True if try succeeded |
| `try_error` | bool | True if catch executed |

#### Examples

```csharp
// Basic try-catch
.AddNode(new TryCatchNode("SafeAPICall",
tryBuilder: t => t
        .AddNode(new HttpRequestNode("API", apiConfig))
        .AddNode(new LlmNode("Process", llmConfig)),
    
    catchBuilder: c => c
        .AddNode(new LogNode("ErrorLog"))
        .AddStep("Fallback", (data, _) =>
        Task.FromResult(data.Set("result", "Default value")))
))

// Nested error handling
.AddNode(new TryCatchNode("OuterTry",
    tryBuilder: t => t
        .AddNode(new TryCatchNode("InnerTry",
      tryBuilder: inner => inner.AddNode(riskyNode),
     catchBuilder: innerCatch => innerCatch.AddNode(fallback1)
   )),
    catchBuilder: c => c.AddNode(fallback2)
))
```

---

### DelayNode

**Category:** Control  
**Purpose:** Add a pause before the next node

#### Factory Methods

```csharp
DelayNode.Milliseconds(int ms, string? reason = null)
DelayNode.Seconds(int seconds, string? reason = null)
DelayNode.Minutes(int minutes, string? reason = null)
DelayNode.RateLimitDelay(int requestsPerMinute)
```

#### Examples

```csharp
// Rate limiting
.AddNode(new HttpRequestNode("API", config))
.AddNode(DelayNode.Milliseconds(200))  // 200ms pause

// Between loop iterations
.ForEach(
    itemsKey: "items",
    outputKey: "results",
    bodyBuilder: loop => loop
        .AddNode(new ProcessNode())
        .AddNode(DelayNode.Seconds(1))  // 1 second between items
)

// Calculated rate limit
.AddNode(DelayNode.RateLimitDelay(requestsPerMinute: 60))
// Automatically calculates 1000ms delay
```

---

### LogNode

**Category:** Control  
**Purpose:** Logging checkpoint for debugging

#### Factory Methods

```csharp
LogNode.All(string label, LogLevel level = LogLevel.Information)
LogNode.Keys(string label, params string[] keys)
```

#### Examples

```csharp
// Log all WorkflowData
.AddNode(LogNode.All("Checkpoint1"))

// Log specific keys
.AddNode(LogNode.Keys("AfterLLM", "llm_response", "prompt_tokens"))

// With custom log level
.AddNode(new LogNode("Debug", new[] { "debug_info" }, LogLevel.Debug))
```

---

### MergeNode

**Category:** Control  
**Purpose:** Combine multiple WorkflowData keys into one

#### Constructor

```csharp
public MergeNode(
    string name,
    string[] sourceKeys,
    string outputKey,
    string separator = "\n")
```

#### Examples

```csharp
// Merge parallel results
.Parallel(
    new Node1(),  // Outputs "result1"
  new Node2(),  // Outputs "result2"
new Node3()   // Outputs "result3"
)
.AddNode(new MergeNode("Combine",
    sourceKeys: new[] { "result1", "result2", "result3" },
    outputKey: "combined",
    separator: "\n\n---\n\n"
))
```

---

### ErrorRouteNode

**Category:** Control  
**Purpose:** Custom error routing logic

#### Constructor

```csharp
public ErrorRouteNode(
    string name,
  Func<WorkflowData, string> router)
```

The router function returns a route name ("retry", "ignore", "escalate", etc.)

#### Examples

```csharp
.AddNode(new ErrorRouteNode("RouteError", data =>
{
    var errorType = data.GetString("error_type");
    return errorType switch
    {
        "timeout" => "retry",
        "unauthorized" => "reauth",
        "not_found" => "ignore",
     _ => "escalate"
  };
}))
```

---

### LoopNode

**Category:** Control  
**Purpose:** Iterate over a list and execute a sub-workflow per item

#### Constructor

```csharp
public LoopNode(
    string name,
    string itemsKey,
 string outputKey,
  Workflow body)
```

Built via `Workflow.ForEach()` in practice.

#### Examples

```csharp
.ForEach(
    itemsKey: "documents",
    outputKey: "summaries",
  bodyBuilder: doc => doc
        .AddStep("PrepareDoc", (data, _) =>
        {
   var document = data.GetString("__loop_item__");
          return Task.FromResult(data.Set("text", document));
        })
        .AddNode(new PromptBuilderNode("SummarizePrompt", 
            "Summarize: {{text}}"))
        .AddNode(new LlmNode("Summarizer", llmConfig))
)
// Result: "summaries" contains List<WorkflowData> of all iterations
```

---

## IO Nodes

### HttpRequestNode

**Category:** IO  
**Purpose:** Make HTTP requests to external APIs

#### Constructor

```csharp
public HttpRequestNode(
    string name,
    HttpRequestConfig config)
```

#### HttpRequestConfig Properties

```csharp
public class HttpRequestConfig
{
    public string Method { get; set; }
    public string UrlTemplate { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public object? Body { get; set; }
    public TimeSpan Timeout { get; set; }
    public bool ThrowOnError { get; set; }
}
```

#### URL Template Syntax

```
https://api.example.com/users/{{user_id}}/posts/{{post_id}}
```

Variables in `{{}}` are replaced from WorkflowData.

#### Data In (for POST/PUT)

| Key | Type | Description |
|-----|------|-------------|
| `request_body` | object | Used if config.Body is null |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `http_response` | object/string | Parsed JSON or raw string |
| `http_status_code` | int | HTTP status code |
| `http_headers` | Dictionary | Response headers |

#### Examples

```csharp
// GET request
var config = new HttpRequestConfig
{
    Method = "GET",
    UrlTemplate = "https://api.example.com/users/{{user_id}}",
    Headers = new()
    {
        ["Authorization"] = "Bearer {{api_token}}"
    },
    Timeout = TimeSpan.FromSeconds(30)
};

// POST request with static body
var config = new HttpRequestConfig
{
    Method = "POST",
    UrlTemplate = "https://api.example.com/users",
    Headers = new() { ["Content-Type"] = "application/json" },
Body = new
    {
        name = "{{user_name}}",
        email = "{{user_email}}"
    }
};

// POST with dynamic body
.AddStep("BuildBody", (data, _) =>
{
    var body = new { /* dynamic data */ };
    return Task.FromResult(data.Set("request_body", body));
})
.AddNode(new HttpRequestNode("API", new HttpRequestConfig
{
    Method = "POST",
    UrlTemplate = "https://api.example.com/action"
    // Body read from "request_body" WorkflowData key
}))
```

---

### FileReaderNode

**Category:** IO  
**Purpose:** Read files from disk

#### Constructor

```csharp
public FileReaderNode(
    string name,
    string filePath)
```

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `content` | string | File contents |
| `file_path` | string | Path of file read |

#### Examples

```csharp
.AddNode(new FileReaderNode("ReadDoc", "path/to/document.txt"))
```

---

### FileWriterNode

**Category:** IO  
**Purpose:** Write files to disk

#### Constructor

```csharp
public FileWriterNode(
    string name,
    string filePath,
    string contentKey)
```

#### Data In

| Key | Type | Description |
|-----|------|-------------|
| `{contentKey}` | string | Content to write |

#### Examples

```csharp
.AddNode(new FileWriterNode("SaveResult", "output.txt", "llm_response"))
// Writes content of "llm_response" to "output.txt"
```

---

### CsvReadNode

**Category:** IO  
**Purpose:** Parse a CSV string from WorkflowData into a list of row dictionaries (RFC 4180 compliant)

#### Constructor

```csharp
public CsvReadNode(
    string name,
    string csvKey,
    string outputKey = "csv_rows",
    bool hasHeader = true,
    char delimiter = ',')
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `csvKey` | string | Yes | WorkflowData key that holds the CSV string |
| `outputKey` | string | No | Key to write the parsed rows to (default: `csv_rows`) |
| `hasHeader` | bool | No | Treat first row as column names (default: true) |
| `delimiter` | char | No | Field delimiter character (default: `,`) |

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `{csvKey}` | string | Yes | String containing CSV text |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | List&lt;Dictionary&lt;string, string&gt;&gt; | Parsed rows, one dictionary per row |
| `csv_row_count` | int | Number of data rows parsed |
| `csv_columns` | List&lt;string&gt; | Column names (from header row) |

#### Examples

```csharp
// Read a CSV file, then parse it
.AddNode(new FileReaderNode("Read", "data.csv"))
.AddNode(new CsvReadNode("Parse", csvKey: "content", outputKey: "rows"))

// Custom delimiter (tab-separated)
new CsvReadNode("ParseTsv", csvKey: "tsv_text", delimiter: '\t')

// Process rows in a loop
.AddNode(new CsvReadNode("ParseCsv", csvKey: "csv_content"))
.ForEach(itemsKey: "csv_rows", outputKey: "processed", bodyBuilder: row => row
    .AddStep("ExtractField", (data, _) =>
    {
        var record = data.Get<Dictionary<string, string>>("__loop_item__");
        return Task.FromResult(data.Set("email", record?["email"]));
    })
    .AddNode(new LlmNode("Process", llmConfig))
)
```

---

### CsvWriteNode

**Category:** IO  
**Purpose:** Serialize a list of row dictionaries from WorkflowData back to a CSV string (RFC 4180 compliant)

#### Constructor

```csharp
public CsvWriteNode(
    string name,
    string dataKey,
    string outputKey = "csv_output",
    bool includeHeader = true,
    char delimiter = ',')
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | Node instance name |
| `dataKey` | string | Yes | WorkflowData key holding the list of row dicts |
| `outputKey` | string | No | Key to write the CSV string to (default: `csv_output`) |
| `includeHeader` | bool | No | Write a header row (default: true) |
| `delimiter` | char | No | Field delimiter character (default: `,`) |

#### Data In

| Key | Type | Required | Description |
|-----|------|----------|-------------|
| `{dataKey}` | List of dicts/objects | Yes | Row data to serialize |

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `{outputKey}` | string | CSV string |
| `csv_row_count` | int | Number of data rows written |

#### Examples

```csharp
// Write processed rows to CSV
new CsvWriteNode("Write", dataKey: "processed_rows", outputKey: "csv_text")

// Then save to file
.AddNode(new CsvWriteNode("Serialize", dataKey: "results"))
.AddNode(new FileWriterNode("Save", "output.csv", "csv_output"))

// Tab-separated output without header
new CsvWriteNode("WriteTsv", dataKey: "rows", includeHeader: false, delimiter: '\t')
```

---

### GoogleSearchNode

**Category:** IO  
**Purpose:** Search Google via API

#### Constructor

```csharp
public GoogleSearchNode(
    string name,
    SearchConfig config)
```

#### SearchConfig Properties

```csharp
public class SearchConfig
{
    public string ApiKey { get; set; }
 public string SearchEngineId { get; set; }
    public string Query { get; set; }
    public int NumResults { get; set; }
}
```

#### Data Out

| Key | Type | Description |
|-----|------|-------------|
| `search_results` | List<object> | Search result items |

#### Examples

```csharp
.AddNode(new GoogleSearchNode("Research", new SearchConfig
{
    ApiKey = googleApiKey,
SearchEngineId = searchEngineId,
    Query = "{{research_topic}}",
    NumResults = 10
}))
```

---

## Base Classes

### BaseNode

**Purpose:** Abstract base class for all custom nodes

#### Template Method

```csharp
protected abstract Task<WorkflowData> RunAsync(
    WorkflowData input,
    WorkflowContext context,
    NodeExecutionContext nodeCtx);
```

#### Required Overrides

```csharp
public override string Name { get; }
public override string Category { get; }
```

#### Optional Overrides

```csharp
public override string Description { get; }
public override string IdPrefix { get; }
public override IReadOnlyList<NodeData> DataIn { get; }
public override IReadOnlyList<NodeData> DataOut { get; }
```

#### Example

```csharp
public class MyCustomNode : BaseNode
{
    public override string Name => "MyNode";
    public override string Category => "Custom";
    public override string Description => "Does custom processing";
    
  public override IReadOnlyList<NodeData> DataIn =>
    [
     new("input_text", typeof(string), Required: true, 
            Description: "Text to process")
  ];
 
    public override IReadOnlyList<NodeData> DataOut =>
    [
        new("processed_text", typeof(string), 
            Description: "Processed result")
    ];
    
    protected override async Task<WorkflowData> RunAsync(
 WorkflowData input,
        WorkflowContext context,
        NodeExecutionContext nodeCtx)
    {
     nodeCtx.Log("Starting processing");
      
        var text = input.GetRequiredString("input_text");
        var processed = text.ToUpperInvariant();
        
        nodeCtx.SetMetadata("original_length", text.Length);
    nodeCtx.SetMetadata("processed_length", processed.Length);
        
  return input.Clone().Set("processed_text", processed);
    }
}
```

---

### SimpleTransformNode

**Purpose:** Simplified base for single-key transformations

#### Required Overrides

```csharp
protected abstract string InputKey { get; }
protected abstract string OutputKey { get; }
protected abstract Task<object?> TransformAsync(object? input, WorkflowContext context);
```

#### Example

```csharp
public class UpperCaseNode : SimpleTransformNode
{
    public override string Name => "UpperCase";
    public override string Category => "Data";
    protected override string InputKey => "text";
    protected override string OutputKey => "text";
    
    protected override Task<object?> TransformAsync(object? input, WorkflowContext context)
    {
        return Task.FromResult<object?>(input?.ToString()?.ToUpperInvariant());
    }
}
```

---

### LambdaNode

**Purpose:** Inline node without creating a class

Created via `Workflow.AddStep()`.

#### Example

```csharp
.AddStep("MyStep", async (data, ctx) =>
{
    var value = data.GetString("input");
    var result = await ProcessAsync(value);
    return data.Set("output", result);
})
```

---

## Summary

This reference covered:

✅ **4 AI Nodes** — LLM, PromptBuilder, OutputParser, Embedding  
✅ **14 Data Nodes** — Transform, Mapper, Filter, Chunking, Memory, Variables, JsonParse, JsonStringify, Math, CreateList, AddListItem, RemoveListItem, MergeLists, AppendString  
✅ **8 Control Nodes** — Condition, Branch, TryCatch, Delay, Log, Merge, ErrorRoute, Loop  
✅ **6 IO Nodes** — HTTP, FileReader, FileWriter, CsvRead, CsvWrite, GoogleSearch  
✅ **3 Base Classes** — BaseNode, SimpleTransformNode, LambdaNode  

For complete examples, see:
- `source/console/examples/` � Production-ready workflows
- `tests/` � Comprehensive test coverage
- `docs/HOW_TO_GUIDE.md` � Step-by-step tutorials

**All nodes support:**
- NodeOptions (retry, timeout, conditions, continue-on-error)
- Logging and tracking
- Cancellation token propagation
- Metadata collection
