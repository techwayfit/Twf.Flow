# Twf.Flow  Solution Architecture

## Overview

**Twf.Flow** is a lightweight, node-based AI workflow automation library for .NET 10, inspired by visual workflow tools like n8n but implemented as an embeddable C# library. It enables developers to build composable AI pipelines through code-first or designer-driven approaches.

---

## Architectural Principles

### 1. **Node-Based Composition**
Every operation is a node implementing `INode`. Nodes are chained together to form workflows. This follows the **Chain of Responsibility** pattern where each node transforms data and passes it to the next.

### 2. **Immutable Data Flow**
`WorkflowData` flows through nodes immutably. Each node receives a snapshot, transforms it, and returns a new copy. This ensures:
- Failed nodes don't corrupt upstream state
- Parallel execution has isolated data
- Debugging is easier with clear data lineage

### 3. **Separation of Concerns**
The framework is organized into distinct layers:

```
+-- |     Application Layer     
|   (Console, Web, Custom Applications)  
+--    
+-- |      Workflow Orchestration      
|   (Workflow, WorkflowBuilder,          
|    WorkflowExecutor)       
+--   
+-- |         Node Execution Layer            
 (BaseNode, INode, Step Executors)    
+--           
+-- |       Domain Nodes    
|   (AI, IO, Data, Control Nodes)        
+--  
+-- |         Infrastructure     
|   (HTTP, Secrets, Sanitization,        
|    Logging, Tracking)   
+-- ```

### 4. **Dependency Injection Friendly**
- Nodes accept infrastructure services through constructor injection (`IHttpClientProvider`, `ISecretProvider`, `IPromptSanitizer`)
- Context uses `ILogger` from Microsoft.Extensions.Logging
- No hard dependencies on specific implementations

### 5. **Retry, Timeout, and Error Handling**
Built-in resilience patterns at multiple levels:
- Per-node retry with exponential backoff
- Per-node timeout with cancellation
- Per-node error continuation
- Workflow-level error handlers
- Nested error boundaries in sub-workflows

---

## Core Components

### 1. Workflow Layer

#### `Workflow` (Facade)
The primary entry point for building and executing workflows. Provides a fluent API that delegates to `WorkflowBuilder` and `WorkflowExecutor`.

**Responsibilities:**
- Fluent workflow construction
- Backward compatibility with legacy API
- Convenient `RunAsync()` method

**Pattern:** Facade

#### `WorkflowBuilder` (Builder)
Constructs immutable workflow structures.

**Responsibilities:**
- Build workflow structure (`WorkflowStructure`)
- Configure workflow-level settings (logger, error strategy, callbacks)
- Create control flow (branches, loops, parallel)

**Pattern:** Builder

#### `WorkflowExecutor` (Strategy/Command)
Executes a workflow structure.

**Responsibilities:**
- Execute workflow steps sequentially
- Delegate to specialized step executors (node, branch, parallel, loop)
- Track execution and collect results
- Handle cancellation and errors

**Pattern:** Strategy (uses `IStepExecutor` implementations)

#### `WorkflowStructure` (Value Object)
Immutable representation of a workflow's structure.

**Responsibilities:**
- Store workflow metadata (name, steps, configuration)
- Provide read-only access to workflow definition

**Pattern:** Value Object / Immutable Data Transfer

---

### 2. Node Layer

#### `INode` (Interface)
The fundamental contract for all workflow operations.

**Contract:**
```csharp
string Name { get; }
string Category { get; }
string Description { get; }
string IdPrefix { get; }
IReadOnlyList<NodeData> DataIn { get; }
IReadOnlyList<NodeData> DataOut { get; }
Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context);
```

**Pattern:** Strategy Pattern (each node is a strategy for data transformation)

#### `BaseNode` (Abstract Base Class)
Template method implementation providing common node infrastructure.

**Responsibilities:**
- Execution timing
- Exception catching
- Logging with node context
- Metadata and log collection

**Pattern:** Template Method

```csharp
// Template method pattern
public async Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context)
{
    var startedAt = DateTime.UtcNow;
    var nodeCtx = new NodeExecutionContext(Name, context.Logger);
    
    try
    {
        var output = await RunAsync(data, context, nodeCtx); //  Hook method
 return NodeResult.Success(...);
    }
    catch (Exception ex)
    {
        return NodeResult.Failure(...);
    }
}

protected abstract Task<WorkflowData> RunAsync(...); //  Must implement
```

#### `SimpleTransformNode` (Specialized Base)
Convenience base for single-key transformation nodes.

**Pattern:** Template Method + Adapter

---

### 3. Data Layer

#### `WorkflowData` (Value Object / Data Bag)
Dynamic, type-safe data packet that flows between nodes.

**Responsibilities:**
- Store key-value pairs with case-insensitive lookup
- Type-safe access with coercion
- Cloning for immutability
- Merging for parallel execution
- JSON serialization

**Key Methods:**
```csharp
T? Get<T>(string key)
T GetRequired<T>(string key)
WorkflowData Set<T>(string key, T value)
WorkflowData Clone()
WorkflowData Merge(WorkflowData other)
```

**Pattern:** Fluent Builder + Value Object

#### `WorkflowContext` (Execution Context)
Scoped execution environment for a workflow run.

**Responsibilities:**
- Provide infrastructure (logger, tracker, cancellation)
- Manage workflow identity (RunId, name, timestamps)
- Global state management (via `IWorkflowState`)
- Chat history management (via extension methods)

**Pattern:** Context Object / Service Locator (deprecated features removed in favor of DI)

#### `IWorkflowState` / `WorkflowState`
Workflow-scoped state storage that persists across nodes.

**Responsibilities:**
- Store global state (unlike `WorkflowData` which is per-step)
- Provide type-safe access to state
- Support chat history extension methods

**Pattern:** Strategy + State Pattern

---

### 4. Execution Engine

#### Step Executors
Specialized executors for different step types:

| Executor | Responsibility |
|----------|----------------|
| `NodeStepExecutor` | Execute a single node with retry, timeout, conditions |
| `BranchStepExecutor` | Evaluate condition and execute one of two branches |
| `ParallelStepExecutor` | Execute multiple nodes concurrently and merge results |
| `LoopStepExecutor` | Iterate over items and execute a sub-workflow per item |
| `DefaultStepExecutor` | Dispatch to appropriate typed executor |

**Pattern:** Strategy + Chain of Responsibility

```
WorkflowExecutor
    +-- DefaultStepExecutor (dispatcher)
    |   +-- NodeStepExecutor
    |   +-- BranchStepExecutor
    |   +-- ParallelStepExecutor
    |   +-- LoopStepExecutor
```

#### Execution Flow

```
+--  RunAsync()  
+--      
+--  Build Structure       WorkflowBuilder.Build()
+--        
+--  Execute Structure     WorkflowExecutor.ExecuteAsync()
+--        
+--  For Each Step  
|   Node Step    NodeStepExecutor
|   Branch Step      BranchStepExecutor
|   Parallel Step    ParallelStepExecutor
|   Loop Step        LoopStepExecutor
+--        
+--  Collect Results     
+--     
+--  Return WorkflowResult
+-- ```

---

### 5. Node Categories

#### AI Nodes (`Twf.Flow.Nodes.AI`)

| Node | Responsibility | Key Dependencies |
|------|----------------|------------------|
| `LlmNode` | Call LLM APIs (OpenAI, Anthropic, Ollama, Azure) | `IHttpClientProvider`, `ISecretProvider`, `IPromptSanitizer` |
| `PromptBuilderNode` | Template-based prompt construction | None |
| `OutputParserNode` | Extract structured JSON from LLM responses | None |
| `EmbeddingNode` | Generate vector embeddings | `IHttpClientProvider`, `ISecretProvider` |

**Pattern:** Strategy (each provider is a strategy)

#### IO Nodes (`Twf.Flow.Nodes.IO`)

| Node | Responsibility | Key Dependencies |
|------|----------------|------------------|
| `HttpRequestNode` | Make HTTP API calls | `IHttpClientProvider` |
| `FileReaderNode` | Read files from disk | File system |
| `FileWriterNode` | Write files to disk | File system |
| `GoogleSearchNode` | Web search integration | External API |

#### Data Nodes (`Twf.Flow.Nodes.Data`)

| Node | Responsibility |
|------|----------------|
| `TransformNode` | Apply custom transformations |
| `DataMapperNode` | Map keys from one schema to another |
| `FilterNode` | Validate data against conditions |
| `ChunkTextNode` | Split text into overlapping chunks |
| `MemoryNode` | Read/write global state |
| `SetVariableNode` | Set workflow variables |

#### Control Nodes (`Twf.Flow.Nodes.Control`)

| Node | Responsibility |
|------|----------------|
| `ConditionNode` | Evaluate boolean conditions |
| `BranchNode` | Switch/case routing |
| `TryCatchNode` | Exception handling |
| `DelayNode` | Add delays for rate limiting |
| `LogNode` | Logging checkpoints |
| `MergeNode` | Aggregate multiple keys |
| `ErrorRouteNode` | Error routing logic |
| `LoopNode` | Iteration over collections |

---

### 6. Infrastructure Layer

#### HTTP Management
```
IHttpClientProvider
  +-- DefaultHttpClientProvider (single shared client)
  +-- PooledHttpClientProvider (connection pooling, future)
```

**Pattern:** Factory + Singleton

#### Secret Management
```
ISecretProvider
  +-- DefaultSecretProvider (environment variables)
  +-- Custom implementations (Azure Key Vault, AWS Secrets Manager, etc.)
```

**Pattern:** Strategy

#### Prompt Sanitization
```
IPromptSanitizer
  +-- DefaultPromptSanitizer (SQL injection, XSS, PII detection)
  +-- Custom implementations
```

**Pattern:** Strategy + Chain of Responsibility (multiple sanitization rules)

#### Tracking
```
ExecutionTracker
  +-- NodeExecutionRecord (per-node tracking)
  +-- WorkflowReport (aggregated reporting)
```

**Pattern:** Observer + Collector

---

## Data Flow Architecture

### Per-Step Data Flow

```
+--  WorkflowData     Input from previous node
+--          
+--  Node.ExecuteAsync()
|   - Read keys from WorkflowData
|   - Execute logic
|   - Write new keys to WorkflowData
+--          
+--  WorkflowData     Output to next node (cloned + updated)
+-- ```

### Global State vs. Per-Step Data

| Aspect | WorkflowData | WorkflowContext.State |
|--------|--------------|----------------------|
| **Scope** | Per-step | Entire workflow run |
| **Lifetime** | Cloned per node | Persists across all nodes |
| **Use Case** | Data transformation pipeline | Session state, configuration, chat history |
| **Access Pattern** | `data.Get/Set` | `context.State.Get/Set` |

---

## Error Handling Architecture

### Layer 1: Node-Level Resilience
```csharp
NodeOptions.WithRetry(maxRetries: 3, delay: TimeSpan.FromSeconds(2))
    .AndTimeout(TimeSpan.FromSeconds(30))
  .AndContinueOnError()
```

**Pattern:** Decorator (NodeOptions wraps node execution with retry/timeout)

### Layer 2: Workflow-Level Error Handler
```csharp
.OnError((nodeName, exception) => {
    // Log error, send alert, etc.
})
```

**Pattern:** Observer

### Layer 3: Try-Catch Composition
```csharp
new TryCatchNode("SafeCall",
    tryBuilder: t => t.AddNode(riskyNode),
  catchBuilder: c => c.AddNode(fallbackNode))
```

**Pattern:** Composite + Template Method

---

## Concurrency Model

### Parallel Execution
```csharp
.Parallel(
    new SentimentNode(),
    new KeywordExtractorNode(),
    new CategoryClassifierNode()
)
```

**Implementation:**
- Each node receives a cloned `WorkflowData`
- All nodes execute concurrently via `Task.WhenAll`
- Results are merged back (later keys overwrite earlier)

**Pattern:** Fork-Join

### Loop Execution
```csharp
.ForEach(
    itemsKey: "documents",
    outputKey: "summaries",
    bodyBuilder: b => b.AddNode(new SummarizeNode())
)
```

**Implementation:**
- Sequential iteration (not parallel by default)
- Each item gets its own `WorkflowData` context
- Results collected into a list

**Pattern:** Iterator + Template Method

---

## Value Objects

The framework uses value objects for domain concepts:

| Value Object | Purpose | Validation |
|--------------|---------|------------|
| `Temperature` | LLM temperature (0.0-2.0) | Range check |
| `TokenCount` | Token limits and usage | Positive integer |
| `ChunkSize` | Text chunk size | Positive integer |
| `ChunkOverlap` | Chunk overlap size | Non-negative, less than chunk size |

**Pattern:** Value Object + Factory Method

**Benefits:**
- Type safety (can't accidentally pass raw `float` where `Temperature` expected)
- Validation at creation (invalid values rejected immediately)
- Self-documenting code

---

## Extension Points

### 1. Custom Nodes
Implement `INode` or extend `BaseNode`:

```csharp
public class MyCustomNode : BaseNode
{
    public override string Name => "MyNode";
    public override string Category => "Custom";
    
    protected override async Task<WorkflowData> RunAsync(
        WorkflowData input, 
        WorkflowContext context, 
        NodeExecutionContext nodeCtx)
    {
        // Custom logic here
        return input.Clone().Set("result", processedValue);
    }
}
```

### 2. Custom Infrastructure Providers
Implement infrastructure interfaces:

```csharp
public class AzureKeyVaultSecretProvider : ISecretProvider
{
    public async Task<string?> ResolveSecretAsync(string reference)
    {
        // Azure Key Vault integration
    }
}
```

### 3. Custom Step Executors
Implement `IStepExecutor` for new step types:

```csharp
public class CustomStepExecutor : IStepExecutor
{
    public async Task<StepExecutionResult> ExecuteAsync(
        PipelineStep step, 
    WorkflowData data, 
  WorkflowContext context)
    {
  // Custom execution logic
    }
}
```

---

## Performance Considerations

### Memory
- **WorkflowData cloning**: Uses shallow copy of dictionary; values are references
- **Parallel execution**: Each parallel branch gets a cloned data bag
- **Large data**: Store references, not full data, in WorkflowData when possible

### HTTP Connections
- **DefaultHttpClientProvider**: Single shared `HttpClient` (recommended for most cases)
- **PooledHttpClientProvider**: Available for advanced scenarios with connection pooling

### Streaming
- **LLM streaming**: SSE (Server-Sent Events) support for real-time token streaming
- **OnChunk callback**: Invoked per text chunk without buffering full response

---

## Testing Architecture

### Unit Testing
- **TestNode** helper for creating test doubles
- **Mock infrastructure providers** via `NSubstitute`
- **Fluent assertions** via `FluentAssertions`

### Integration Testing
- **End-to-end workflow tests** with real nodes
- **Mock LLM responses** for deterministic tests
- **In-memory HTTP clients** for API testing

### Test Structure
```
tests/
  +-- Core/  # Core framework tests
  |   +-- WorkflowDataTests.cs
  |   +-- NodeOptionsTests.cs
  |   +-- WorkflowBuilderTests.cs
  |   +-- ...
  +-- Nodes/          # Node-specific tests
  |   +-- BaseNodeTests.cs
  |   +-- ...
  +-- Integration/       # Integration tests
      +-- EndToEndWorkflowTests.cs
      +-- ValueObjectIntegrationTests.cs
```

---

## Design Patterns Summary

| Pattern | Where Used | Purpose |
|---------|------------|---------|
| **Builder** | `WorkflowBuilder`, `NodeOptions` | Fluent API for construction |
| **Facade** | `Workflow` | Simplified entry point |
| **Strategy** | `INode`, `IStepExecutor`, infrastructure providers | Pluggable algorithms |
| **Template Method** | `BaseNode` | Common execution flow with hook points |
| **Chain of Responsibility** | Workflow execution, sanitization | Sequential processing |
| **Observer** | `OnComplete`, `OnError`, `ExecutionTracker` | Event notification |
| **Factory** | Value objects, `LlmConfig` | Object creation with validation |
| **Value Object** | `Temperature`, `TokenCount`, `WorkflowData` | Immutable domain values |
| **Composite** | Sub-workflows, nested branches | Tree structures |
| **Decorator** | `NodeOptions` (retry/timeout) | Add behavior without modifying node |
| **Fork-Join** | Parallel execution | Concurrent task execution |

---

## Future Enhancements

### Planned Features
1. **Visual Designer**: Web-based workflow designer (already in development)
2. **Caching Layer**: HTTP response and LLM response caching
3. **Workflow Versioning**: Version control for workflow definitions
4. **Distributed Execution**: Execute nodes across multiple machines
5. **Plugin System**: Dynamic node loading from assemblies
6. **Workflow Marketplace**: Share and reuse workflow templates

### Extensibility Points
- Custom node categories
- Custom step types
- Custom data serialization
- Custom execution strategies
- Custom tracking and observability

---

## References

- [n8n Workflow Automation](https://n8n.io/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
