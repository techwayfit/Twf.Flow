# Node Examples

This directory contains comprehensive examples for every node type in Twf.Flow.

## Directory Structure

```
node-examples/
+-- AINodeExamples.cs       - AI-category nodes
+-- ControlNodeExamples.cs  - Control flow nodes
+-- DataNodeExamples.cs     - Data manipulation nodes
+-- IONodeExamples.cs  - Input/Output nodes
```

## Example Categories

### 1. AI Nodes (`AINodeExamples.cs`)

Examples for AI-powered nodes that integrate with LLM providers:

- **LlmNode**: Call any OpenAI-compatible LLM (OpenAI, Anthropic, Ollama, Azure)
  - Simple prompt completion
  - System prompts
  - Token usage tracking

- **EmbeddingNode**: Generate vector embeddings for RAG and semantic search
  - Single text embedding
  - Batch embedding
  - Dimension inspection

- **PromptBuilderNode**: Build dynamic prompts with {{variable}} substitution
  - Template-based prompts
  - System + user prompts
  - Static variables

- **OutputParserNode**: Extract structured JSON from LLM responses
  - JSON parsing with field mapping
  - Markdown code fence extraction
  - Strict vs. lenient parsing

**Usage**: Run from console menu option **9**  
**Requires**: API key for OpenAI or Anthropic

### 2. Control Nodes (`ControlNodeExamples.cs`)

Examples for workflow control flow and orchestration:

- **BranchNode**: Route by value (switch/case pattern)
  - Order routing by status
  - Multiple case handlers
  - Default fallback

- **ConditionNode**: Evaluate conditions and set boolean flags
  - Discount eligibility checks
  - Multiple condition evaluation
  - Conditional routing support

- **LoopNode**: Iterate over collections (ForEach)
  - Email batch processing
  - Per-item validation
  - Result collection

- **TryCatchNode**: Error handling with fallback workflows
  - Resilient API calls
  - Cached fallback
  - Error recovery patterns

- **DelayNode**: Introduce pauses in workflows
  - Rate limiting
  - API throttling
  - Timed intervals

- **LogNode**: Add logging checkpoints
  - Debug workflows
  - Selective key logging
  - Full data snapshots

- **MergeNode**: Combine multiple data keys
  - Report section concatenation
  - String merging
  - Parallel result aggregation

- **ErrorRouteNode**: Route by error indicators
  - HTTP status code routing
  - Error message detection
  - Success/error path handling

**Usage**: Run from console menu option **10**  
**Requires**: No API keys needed

### 3. Data Nodes (`DataNodeExamples.cs`)

Examples for data transformation and manipulation:

- **ChunkTextNode**: Split large text for RAG
  - Character-based chunking
  - Word-based chunking
  - Sentence-based chunking
  - Overlapping chunks

- **DataMapperNode**: Map source keys to target keys
  - API response transformation
  - Nested data extraction (dot-path)
  - Default value fallbacks
  - Clean output mode

- **FilterNode**: Validate data with custom rules
  - User registration validation
  - Email format checks
  - Business rule enforcement
  - Soft vs. strict validation

- **TransformNode**: Apply custom transformations
  - Data cleaning (trim, case conversion)
  - Prebuilt transforms (rename, concat)
  - Lambda-based logic

- **SetVariableNode**: Set literal or interpolated values
  - Variable initialization
  - Template interpolation
  - Dynamic value assignment

- **MemoryNode**: Read/write workflow state
  - Multi-turn conversations
  - Session persistence
  - User preference storage

**Usage**: Run from console menu option **11**  
**Requires**: No API keys needed

### 4. I/O Nodes (`IONodeExamples.cs`)

Examples for external system integration:

- **HttpRequestNode**: Make HTTP/REST API calls
  - GET requests with URL templates
  - POST requests with JSON bodies
  - Custom headers
  - Error handling

- **FileReaderNode**: Read files from disk
  - Document loading
  - Dynamic file paths
  - File metadata extraction

- **FileWriterNode**: Write data to files
  - Report generation
  - Template-based paths
  - Directory creation

- **GoogleSearchNode**: Search Google via SerpApi
  - Web research
  - Result limiting
  - Structured search results
  - RAG context enrichment

**Usage**: Run from console menu option **12**  
**Requires**: SerpApi key for Google Search (optional, other examples work without keys)

## Running the Examples

### From the Console Application

1. Build and run the console project:
 ```bash
 cd source/console
   dotnet run
   ```

2. Select the appropriate menu option:
   - **9**: AI Nodes
   - **10**: Control Nodes
   - **11**: Data Nodes
   - **12**: I/O Nodes

3. For examples requiring API keys, you can:
   - Enter the key when prompted
   - Set environment variable `AI_API_KEY`
   - For Google Search: provide SerpApi key when prompted

### From Code

Each example class has a `RunAllExamples()` method:

```csharp
using Twf.Flow.Console.NodeExamples;

// AI nodes (requires API key)
await AINodeExamples.RunAllExamples(apiKey);

// Control nodes (no key needed)
await ControlNodeExamples.RunAllExamples();

// Data nodes (no key needed)
await DataNodeExamples.RunAllExamples();

// I/O nodes (Google Search requires SerpApi key)
await IONodeExamples.RunAllExamples(serpApiKey: null);
```

## Learning Path

### Beginners
1. Start with **Control Nodes** to understand workflow basics
2. Move to **Data Nodes** for data manipulation patterns
3. Then **I/O Nodes** for external integrations
4. Finally **AI Nodes** for LLM integration

### Advanced Users
Each example demonstrates:
- Best practices for that node type
- Common use cases
- Error handling patterns
- Integration with other nodes

## Example Patterns

### Pattern 1: Validation ? Transform ? Action
```csharp
workflow
    .AddNode(new FilterNode("Validate").RequireNonEmpty("email"))
    .AddNode(new TransformNode("Clean", data => data.Set("email", email.ToLower())))
    .AddNode(new HttpRequestNode("SendEmail", httpConfig))
```

### Pattern 2: Parallel Processing ? Merge
```csharp
workflow
    .Parallel(
    new LlmNode("Model1", config1),
        new LlmNode("Model2", config2))
    .AddNode(new MergeNode("Combine", "result", "\n\n", "response1", "response2"))
```

### Pattern 3: Loop with Error Handling
```csharp
workflow
    .AddNode(new LoopNode("ProcessBatch", itemsKey: "items", bodyBuilder: loop => loop
        .AddNode(new TryCatchNode("SafeProcess",
        tryBuilder: w => w.AddNode(processor),
    catchBuilder: w => w.AddStep("LogError", ...)))
    ))
```

## API Keys

### OpenAI
- Sign up: https://platform.openai.com/
- Models: GPT-4, GPT-3.5-turbo, text-embedding-3-small

### Anthropic
- Sign up: https://console.anthropic.com/
- Models: Claude 3.5 Sonnet, Claude 3 Opus

### SerpApi (Google Search)
- Sign up: https://serpapi.com/
- Free tier: 100 searches/month
- Required for GoogleSearchNode examples only

## Further Reading

- [Creating Custom Nodes](../../docs/creating-a-new-node.md)
- [Workflow Patterns](../concepts/)
- [Complete Examples](../examples/)

## Contributing

To add a new node example:

1. Add the example method to the appropriate category file
2. Update the `RunAllExamples()` method to include it
3. Document the use case and pattern in this README
4. Ensure the example runs without errors
