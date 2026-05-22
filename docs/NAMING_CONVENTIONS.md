# Twf.Flow - Naming Conventions

**Version:** 1.0  
**Last Updated:** January 2025  
**Status:** ? Standardized

---

## Overview

This document establishes consistent naming conventions across the Twf.Flow to improve code readability, maintainability, and developer experience.

---

## 1. Method Naming Conventions

### 1.1 Commands (Mutating Methods)

**Pattern:** Verb + Noun (imperative form)

| Verb | Usage | Example |
|------|-------|---------|
| `Add` | Insert item into collection | `AddNode()`, `AddMessage()`, `AddStep()` |
| `Set` | Assign/update a value | `SetState()`, `SetMetadata()`, `SetProperty()` |
| `Remove` | Delete from collection | `RemoveNode()`, `RemoveConnection()` |
| `Delete` | Permanent removal | `DeleteNode()`, `DeleteVariable()` |
| `Clear` | Empty collection/state | `ClearState()`, `ClearHistory()`, `ClearCanvas()` |
| `Update` | Modify existing | `UpdateNodeLabel()`, `UpdateProperty()` |
| `Register` | Add to registry | `RegisterService()`, `RegisterExecutor()` |
| `Create` | Factory/instantiation | `Create()`, `CreateNode()` |
| `Build` | Construct complex object | `Build()`, `BuildStructure()` |
| `Execute` | Perform action | `ExecuteAsync()`, `ExecuteStep()` |
| `Run` | Start workflow/process | `RunAsync()`, `RunWorkflow()` |

**? Consistent Examples:**
```csharp
// Good
public void AddMessage(ChatMessage message)
public void SetState<T>(string key, T value)
public void ClearHistory()
public void RegisterService<T>(T service)

// ? Avoid
public void AppendMessage(...)  // Use Add instead
public void UpdateState(...) // Use Set instead
public void ResetHistory()      // Use Clear instead
```

---

### 1.2 Queries (Non-Mutating Methods)

**Pattern:** Verb + Noun / Question form

| Verb | Usage | Example |
|------|-------|---------|
| `Get` | Retrieve single item | `GetNode()`, `GetState()`, `GetValue()` |
| `Find` | Search for item | `FindNode()`, `FindByType()` |
| `List` | Retrieve collection | `ListNodes()`, `ListVariables()` |
| `TryGet` | Safe retrieval | `TryGet<T>()`, `TryGetValue()` |
| `Has` | Existence check | `Has()`, `HasKey()`, `HasState()` |
| `Is` | Boolean check | `IsSuccess`, `IsFailure`, `IsValid` |
| `Can` | Capability check | `CanExecute()`, `CanRetry()` |
| `Should` | Condition check | `ShouldExecute()`, `ShouldRetry()` |

**? Consistent Examples:**
```csharp
// Good
public T? GetState<T>(string key)
public List<ChatMessage> GetHistory()
public bool Has(string key)
public bool IsSuccess { get; }

// ? Avoid
public List<ChatMessage> ChatHistory()     // Use GetHistory() or GetChatHistory()
public List<ChatMessage> FetchMessages()   // Use GetMessages() or ListMessages()
public bool Contains(string key)           // Use Has() for consistency
```

---

### 1.3 Event Handlers

**Pattern:** `On` + Event / `Handle` + Event

```csharp
// Good
public Workflow OnComplete(Action<WorkflowResult> handler)
public Workflow OnError(Action<string, Exception?> handler)
protected void OnNodeExecuted(NodeResult result)

// Internal handlers
private void HandleNodeResult(NodeResult result)
private void HandleConnectionClick(ConnectionId id)
```

---

## 2. Property and Field Naming

### 2.1 Properties

| Pattern | Usage | Example |
|---------|-------|---------|
| Noun/Adjective | Public properties | `Name`, `Category`, `IsSuccess` |
| `Is` + Adjective | Boolean properties | `IsSuccess`, `IsFailure`, `IsValid` |
| `Has` + Noun | Boolean existence | `HasErrors`, `HasValue` |
| `Can` + Verb | Boolean capability | `CanRetry`, `CanExecute` |

**? Examples:**
```csharp
public string Name { get; }
public string Category { get; }
public bool IsSuccess { get; }
public bool HasErrors { get; }
public int MaxRetries { get; }
public Temperature Temperature { get; init; } = Temperature.Balanced;
```

---

### 2.2 Private Fields

**Pattern:** `_` + camelCase

```csharp
private readonly string _name;
private readonly List<PipelineStep> _steps;
private ILogger _logger = NullLogger.Instance;
private readonly Dictionary<string, object?> _state;
```

---

### 2.3 Constants

**Pattern:** PascalCase (or SCREAMING_SNAKE_CASE for magic numbers)

```csharp
public const int DefaultMaxRetries = 3;
public const string DefaultApiEndpoint = "https://api.openai.com";

private const int MAX_RETRIES = 10;
private const string DEFAULT_MODEL = "gpt-4o";
```

---

## 3. Parameter Naming

### 3.1 Method Parameters

**Pattern:** camelCase descriptive nouns

```csharp
public Workflow AddNode(INode node, NodeOptions options)
public WorkflowData Set<T>(string key, T value)
public Task<NodeResult> ExecuteAsync(WorkflowData data, WorkflowContext context)
```

---

### 3.2 Avoid Abbreviations

**? Good:**
```csharp
WorkflowContext context
WorkflowData data
NodeOptions options
CancellationToken cancellationToken
```

**? Avoid:**
```csharp
WorkflowContext ctx     // Use full name
WorkflowData d    // Use 'data'
NodeOptions opts        // Use 'options'
CancellationToken ct    // Use 'cancellationToken'
```

**Exception:** Commonly accepted abbreviations are OK in local scopes:
```csharp
var temp = Temperature.FromValue(0.7f);  // OK for local variables
foreach (var kvp in dictionary) { ... }  // OK for KeyValuePair
```

---

## 4. Class and Interface Naming

### 4.1 Classes

**Pattern:** Noun / Noun Phrase (PascalCase)

| Type | Pattern | Example |
|------|---------|---------|
| Service classes | Service suffix | `WorkflowExecutor`, `NodeFactory` |
| Manager classes | Manager suffix | `VariableManager`, `StateManager` |
| Handler classes | Handler suffix | `ErrorHandler`, `EventHandler` |
| Provider classes | Provider suffix | `HttpClientProvider`, `LoggerProvider` |
| Builder classes | Builder suffix | `WorkflowBuilder`, `PromptBuilder` |
| Node classes | Node suffix | `LlmNode`, `FilterNode`, `HttpNode` |
| Value objects | Descriptive noun | `Temperature`, `TokenCount`, `ChunkSize` |

---

### 4.2 Interfaces

**Pattern:** `I` + Noun (PascalCase)

```csharp
public interface INode
public interface INodeExecutor
public interface IStepExecutor
public interface ITypedStepExecutor
public interface IWorkflowState
public interface IHttpClientProvider
```

---

### 4.3 Abstract Classes

**Pattern:** `Base` + Noun or Descriptive Noun

```csharp
public abstract class BaseNode
public abstract class WorkflowException
public abstract class NodeException
```

---

## 5. Namespace Naming

**Pattern:** Company.Product.Feature

```csharp
Twf.Flow.Core
Twf.Flow.Core.Execution
Twf.Flow.Core.ValueObjects
Twf.Flow.Core.Exceptions
Twf.Flow.Nodes.AI
Twf.Flow.Nodes.Data
Twf.Flow.Nodes.Control
Twf.Flow.Web.Services
Twf.Flow.Web.Repositories
```

---

## 6. File Naming

**Pattern:** Match class name exactly

```
WorkflowBuilder.cs
LlmNode.cs
Temperature.cs
IStepExecutor.cs
WorkflowException.cs
```

---

## 7. Async Method Naming

**Pattern:** Method + `Async` suffix

```csharp
public async Task<WorkflowResult> RunAsync(...)
public async Task<NodeResult> ExecuteAsync(...)
public async Task<StepExecutionResult> ExecuteStepAsync(...)
public Task<T?> GetStateAsync<T>(string key)
```

---

## 8. Test Method Naming

**Pattern:** `MethodName_Should_ExpectedBehavior_When_Condition`

```csharp
[Fact]
public void WorkflowData_Should_Return_Null_When_Key_Does_Not_Exist()

[Fact]
public async Task NodeExecutor_Should_Retry_On_Failure_When_Retry_Configured()

[Fact]
public void Temperature_Should_Throw_When_Value_Exceeds_Maximum()
```

---

## 9. Event Naming

**Pattern:** Noun Phrase (past tense) / Verb + ing

```csharp
public event EventHandler<NodeExecutedEventArgs> NodeExecuted;
public event EventHandler<WorkflowStartingEventArgs> WorkflowStarting;
public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;
```

---

## 10. Exception Naming

**Pattern:** Descriptive + `Exception` suffix

```csharp
public class WorkflowException : Exception
public class NodeExecutionException : WorkflowException
public class WorkflowDataMissingKeyException : WorkflowException
public class NodeConfigurationException : WorkflowException
public class ValidationException : Exception
```

---

## 11. Enum Naming

### 11.1 Enum Type

**Pattern:** Singular noun (PascalCase)

```csharp
public enum StepType
public enum LlmProvider
public enum ChunkStrategy
public enum GlobalErrorStrategy
```

---

### 11.2 Enum Members

**Pattern:** PascalCase descriptive values

```csharp
public enum StepType
{
    Node,
    Branch,
    Parallel,
    Loop
}

public enum LlmProvider
{
    OpenAI,
    Anthropic,
    Ollama,
    AzureOpenAI
}
```

---

## 12. Fluent API Conventions

**Pattern:** Return `this` or builder type, use verbs

```csharp
// Builder pattern
public WorkflowBuilder AddNode(INode node) => ...;
public WorkflowBuilder UseLogger(ILogger logger) => ...;
public WorkflowBuilder OnComplete(Action<WorkflowResult> handler) => ...;

// Configuration pattern
public static NodeOptions WithRetry(int maxRetries, TimeSpan delay) => ...;
public NodeOptions AndTimeout(TimeSpan timeout) => ...;
public NodeOptions AndContinueOnError(WorkflowData? fallbackData = null) => ...;

// Filter pattern
public FilterNode RequireNonEmpty(string key) => ...;
public FilterNode MaxLength(string key, int maxLength) => ...;
public FilterNode Custom(string key, Func<WorkflowData, bool> predicate, string errorMessage) => ...;
```

---

## 13. Generic Type Parameter Naming

**Pattern:** Single letter or `T` + Descriptive

```csharp
// Single letter for simple generics
public T? Get<T>(string key)
public void Set<T>(string key, T value)

// Descriptive for complex scenarios
public interface IRepository<TEntity, TKey>
public class Cache<TKey, TValue>
```

---

## 14. Lambda Parameter Naming

**Pattern:** Short descriptive names

```csharp
// Good
.AddStep("Transform", (data, ctx) => ...)
.Where(node => node.IsSuccess)
.Select(item => item.Name)
.OnSuccess(result => Console.WriteLine(result))

// ? Avoid single letters when meaning unclear
.AddStep("Transform", (d, c) => ...)  // Not clear what 'd' and 'c' are
```

---

## 15. Collection Naming

**Pattern:** Plural nouns

```csharp
public List<INode> Nodes { get; }
public IReadOnlyList<PipelineStep> Steps { get; }
public Dictionary<string, object?> Variables { get; }
public IEnumerable<NodeResult> NodeResults { get; }
```

---

## 16. Boolean Expression Naming

**Pattern:** Positive phrasing

**? Good:**
```csharp
bool isValid
bool hasErrors
bool canRetry
bool shouldExecute
```

**? Avoid:**
```csharp
bool isInvalid     // Use !isValid instead
bool hasNoErrors   // Use !hasErrors instead
bool cantRetry     // Use !canRetry instead
```

---

## 17. Configuration Object Naming

**Pattern:** Descriptive + `Config`/`Configuration`/`Options`

```csharp
public record LlmConfig
public record ChunkConfig
public record EmbeddingConfig
public class NodeOptions
public class WorkflowConfiguration
```

---

## 18. Value Object Naming

**Pattern:** Domain concept noun

```csharp
public readonly record struct Temperature
public readonly record struct TokenCount
public readonly record struct ChunkSize
public readonly record struct ChunkOverlap
```

---

## 19. Extension Method Naming

**Pattern:** Verb + Noun (descriptive of action)

```csharp
public static class WorkflowDataExtensions
{
    public static string GetRequiredString(this WorkflowData data, string key)
    public static WorkflowData SetIfNotNull<T>(this WorkflowData data, string key, T? value)
}

public static class LoggerExtensions
{
    public static IDisposable BeginWorkflowScope(this ILogger logger, Guid workflowId, string workflowName)
    public static void LogPerformanceMetric(this ILogger logger, string metric, double value)
}
```

---

## 20. Consistency Checklist

Before committing code, ensure:

- [ ] Methods follow verb + noun pattern
- [ ] Boolean properties start with `Is`, `Has`, or `Can`
- [ ] Private fields use `_` prefix
- [ ] Async methods end with `Async`
- [ ] Test methods follow `MethodName_Should_ExpectedBehavior_When_Condition`
- [ ] No abbreviations in public APIs (except widely accepted ones)
- [ ] Fluent APIs return `this` or builder type
- [ ] Collection properties use plural nouns
- [ ] Value objects use descriptive domain nouns
- [ ] Exceptions end with `Exception` suffix

---

## 21. Migration Guide

### Previously Inconsistent ? Standardized

| Old Pattern | New Standard | Status |
|-------------|--------------|--------|
| `AppendMessage()` | `AddMessage()` | ? Keep existing for compatibility |
| `ChatHistory()` | `GetChatHistory()` | ? Updated |
| `ResetHistory()` | `ClearChatHistory()` | ? Updated |

---

## 22. Tools and Linters

**Recommended:**
- **StyleCop Analyzers** - Enforce naming conventions
- **Roslynator** - Suggest improvements
- **SonarLint** - Code quality and consistency

**EditorConfig Example:**

```ini
[*.cs]

# Naming conventions
dotnet_naming_rule.private_members_with_underscore.symbols = private_fields
dotnet_naming_rule.private_members_with_underscore.style = prefix_underscore
dotnet_naming_rule.private_members_with_underscore.severity = warning

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.prefix_underscore.capitalization = camel_case
dotnet_naming_style.prefix_underscore.required_prefix = _
```

---

## 23. References

- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [C# Identifier Naming Rules](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)

---

**Document maintained by:** Twf.Flow Team  
**Contributions:** Submit PRs to update conventions based on team consensus
