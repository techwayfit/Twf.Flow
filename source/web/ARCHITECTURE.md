# Twf.Flow - Web Application Architecture

## System Architecture Overview

This document provides detailed architectural diagrams and design patterns used in the Twf.Flow Web Application.

---

## Table of Contents
- [Layer Architecture](#layer-architecture)
- [Service Dependencies](#service-dependencies)
- [Workflow Execution Flow](#workflow-execution-flow)
- [Data Flow](#data-flow)
- [Database Schema](#database-schema)
- [Design Patterns](#design-patterns)
- [Concurrency Model](#concurrency-model)
- [Error Handling Architecture](#error-handling-architecture)

---

## Layer Architecture

### Clean Architecture Layers

```
+-- |    Presentation Layer      ?
+-- ?  Controllers       ?  Views ?Middleware   ?
|  � Workflow        ?  � Designer UI?  � Exception  ?
|  � Node   ?  � Runner  ?  � CORS       ?
|  � Runner   ?  � Index         ?  � Auth       ?
|        ?  � Details       |        ?
+--        |      ?
+-- |    Business Logic Layer          ?
+-- ?  Services           ?  Domain Models            ?
|  � WorkflowRunner        ?  � WorkflowDefinition     ?
|  � GraphWalker     ?  � NodeDefinition   ?
|  � NodeExecutor          ?  � WorkflowInstance       ?
|  � NodeFactory       ?  � WorkflowRunResult?
|  � VariableResolver      |      ?
|  � SchemaProvider      |    ?
+--             ?
+-- ?  Data Access Layer      ?
+-- ?  Repositories       ?  DbContext       ?
|  � IWorkflowRepository ?  � WorkflowDbContext        ?
|  � INodeTypeRepository |    ?
|  � IInstanceRepository ?  Entities        ?
|  � IUnitOfWork         ?  � WorkflowEntity       ?
|          ?  � NodeTypeEntity     ?
|     ?  � InstanceEntity   ?
+--        ?
+-- |          Infrastructure Layer        ?
+-- ?  Database       ?  External Services           ?
|  � SQLite              ?  � HTTP Client Pool         ?
|  � EF Core             ?  � Logging Infrastructure   ?
|  � Migrations          ?  � Configuration            ?
+-- ```

### Dependency Flow

```
Presentation ? Business Logic ? Data Access ? Infrastructure
     ? |             |       ?
     +--      (Dependency Injection)
```

**Key Principles:**
- Inner layers don't depend on outer layers
- Dependencies point inward
- Interfaces in inner layers, implementations in outer
- Core domain logic is framework-agnostic

---

## Service Dependencies

### Dependency Graph

```
WorkflowRunnerController
        ?
        +-- IWorkflowRepository
        +-- IWorkflowInstanceRepository
        ?
  +-- WorkflowDefinitionRunner
       ?
   +-- IWorkflowGraphWalker
     |   ?
         |       +-- INodeFactory
       |       ? ?
     |    |     +-- INodeSchemaProvider
             |       ?
                ?  +-- INodeExecutor
                    |       |       ?
         |     |       +-- IVariableResolver
       |       ?
   |       +-- ILogger
           ?
    +-- ILogger

WorkflowController
      ?
        +-- IWorkflowRepository
     +-- INodeTypeRepository
        +-- ILogger

NodeController
        ?
        +-- INodeTypeRepository
     +-- ILogger
```

### Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IHttpClientProvider` | Singleton | Connection pooling, DNS refresh |
| `IVariableResolver` | Singleton | Stateless, reusable |
| `INodeFactory` | Singleton | Stateless factory |
| `INodeSchemaProvider` | Singleton | Schema caching |
| `IWorkflowGraphWalker` | Scoped | Per-request execution context |
| `INodeExecutor` | Scoped | Request-scoped logging |
| `WorkflowDefinitionRunner` | Scoped | Per-request orchestration |
| `WorkflowDbContext` | Scoped | EF Core best practice |
| Repositories | Scoped | Database access per request |
| Controllers | Transient | ASP.NET Core default |

---

## Workflow Execution Flow

### Complete Execution Sequence

```
+-- ? 1. HTTP Request: POST /Workflow/Run/{id}  ?
+--       ?
+-- ? 2. WorkflowRunnerController        ?
|    � Parse request body (initialData)    ?
|    � Load WorkflowDefinition from repository    ?
|    � Create WorkflowInstance entity?
|    � Persist instance (status: Running) ?
+--           ?
+-- ? 3. WorkflowDefinitionRunner.RunWithCallbackAsync()  ?
|    � Validate Start node exists            ?
|    � Initialize WorkflowContext      ?
|    � Initialize WorkflowData with initialData    ?
?  � Seed workflow-level variables        ?
? � Build routing table from connections      ?
|    � Create WalkConfiguration  ?
+--    ?
+-- ? 4. WorkflowGraphWalker.WalkAsync()     ?
|    � Start at Start node ?
|    � Enter execution loop (max 500 steps)        ?
|    � For each node:  ?
|      Check node type    ?
|      Handle structural nodes (Start, End, Error)?
|      Handle control flow (Loop, Branch, SubWorkflow)     ?
? Handle regular nodes  ?
+--      ?
    +--  |            ?
    ? (For regular nodes)       ?
    |            ?
+-- ?
? 5. INodeFactory.CreateNode()     ? ?
|    � Lookup node type in registry    ? ?
|    � Instantiate node via reflection     ? ?
? � Return INode instance   ? ?
+-- ?
    |             ?
+-- ?
? 6. IVariableResolver.Resolve()   ? ?
|    � Parse {{variable}} templates      ? ?
|    � Replace with runtime values         ? ?
|    � Support nested keys (node.output)   ? ?
+-- ?
    ?  ?
+-- ?
? 7. NodeStepScope.StartAsync()      ? ?
|    � Emit "node_start" event             ? ?
|    � Capture input snapshot    ? ?
|    � Log structured event        ? ?
+-- ?
    |                   ?
+-- ?
? 8. INodeExecutor.ExecuteAsync()          ? ?
|    � Validate required inputs         ? ?
|    � Apply retry policy     ? ?
|    � Apply timeout policy     ? ?
|    � Execute node.ExecuteAsync()  ? ?
|    � Handle exceptions? ?
|    � Write scoped outputs (nodeId.key)   ? ?
+-- ?
    |        ?
+-- ?
? 9. NodeStepScope.Complete()      ? ?
|    � Emit "node_complete" event          ? ?
|    � Capture output snapshot             ? ?
?  � Log structured event      ? ?
+-- ?
    |   ?
+-- ?
? 10. Route to Next Node       ? ?
|     � Determine activated port  ? ?
?  � Lookup next node in routing table  ? ?
|     � Continue loop or exit              ? ?
+-- ?
  |          ?
    +--     ?
+-- ? 11. Return WalkResult        ?
?  � Success: final WorkflowData    ?
|     � Failure: error message + failed node name           ?
+--      ?
+-- ? 12. WorkflowDefinitionRunner Returns WorkflowRunResult       ?
|     � Map WalkResult to WorkflowRunResult         ?
|     � Log performance metrics    ?
+--      ?
+-- ? 13. WorkflowRunnerController Updates Instance        ?
|     � Set final status (Completed/Failed)                ?
?  � Store output data and error message    ?
|     � Persist to database    ?
+--         ?
+-- ? 14. HTTP Response: WorkflowRunResult JSON   ?
|     � 200 OK (success) or 422 Unprocessable (failure)        ?
|     � Include instanceId for history lookup             ?
+-- ```

### Parallel Execution Paths

#### Standard Execution (Blocking)
```
Request ? Execute Workflow ? Return Result
(blocks until complete)
```

#### Streaming Execution (SSE)
```
Request ? Start SSE Stream ? Execute Workflow ? Close Stream
             |     ?
          Open Connection    Emit Events
       � node_start
           � node_complete
               � node_error
             � workflow_done
```

---

## Data Flow

### Request Data Flow

```
+-- ? HTTP Request ?
| (JSON Body)  ?
+--    ?
       ? { "initialData": { "key": "value" } }
       ?
+-- ? Controller?
| � Parse JSON ?
| � Validate          ?
+--        ?
       ? Dictionary<string, object?>
  ?
+-- ? WorkflowData        ?
| � Set("key", value) ?
+--      ?
  ? WorkflowData instance
       ?
+-- ? WorkflowDefinitionRunner    ?
| � Seed workflow variables   ?
| � Pass to graph walker      ?
+--      ?
+-- ? For each node:    ?
| +--  ?
| ? VariableResolver       ?  ?
| ? � Resolve {{variable}} ?  ?
| ? � Replace in params    ?  ?
| +--  ?
|        |           ?
| +--  ?
| ? Node.ExecuteAsync()    ?  ?
| ? � Read from data      
| ? � Execute logic        ?  ?
| ? � Write to data   ?  ?
| +--  ?
|      |           ?
|    ? WorkflowData  ?
|             ? (mutated)     ?
+--     ?
+-- ? Final WorkflowData    ?
| � All outputs from nodes    ?
| � Scoped outputs (nodeId.*) ?
+--   ?
+-- ? WorkflowRunResult           ?
| � outputData (dict)         ?
| � isSuccess (bool)          ?
+--     ?
+-- ? HTTP Response (JSON)        ?
+-- ```

### Database Data Flow

```
+-- ? WorkflowDefinition    ? (in-memory model)
+--            ?
   ? Serialize to JSON
      ?
+-- ? WorkflowEntity   ? (EF Core entity)
? � Id: Guid     ?
? � Name: string  ?
? � JsonData: string    ? ? full definition as JSON
+--      ?
     ? EF Core SaveChanges()
           ?
+-- ? SQLite Database       ?
? Workflows table       ?
+-- 
(Reverse flow for loading)

+-- ? SQLite Database  ?
? SELECT * FROM ...     ?
+--     ?
+-- ? WorkflowEntity        ?
? JsonData property     ?
+--   ?
   ? Deserialize JSON
  ?
+-- ? WorkflowDefinition?
+-- ```

---

## Database Schema

### Entity Relationship Diagram

```
+-- ? Workflows    ?
+-- ? PK  Id (Guid)  ?
|     Name (string) ?
| Description (string?)     ?
|     JsonData (string)          ? ? Full WorkflowDefinition JSON
|     CreatedAt (DateTime)         ?
|     UpdatedAt (DateTime)       ?
+--              ?
             ? 1:N
 ?
+-- ? WorkflowInstances         ?
+-- ? PK  Id (Guid)                 ?
| FK  WorkflowDefinitionId (Guid) ? ? Workflows.Id
|     WorkflowName (string)        ?
|  Status (string)    ? ? Running/Completed/Failed
|     StartedAt (DateTime)   ?
|     CompletedAt (DateTime?)      ?
|     JsonData (string)   ? ? Full WorkflowInstance JSON
+-- 
+-- ? NodeTypes         ?
+-- ? PK  Id (int)      ?
| UK  NodeType (string)     ? ? "LlmNode"
|     Name (string)      ? ? "LLM"
|     Category (string)         ? ? "AI"
|     Description (string)         ?
|     Color (string)          ?
|     Icon (string)          ?
|     SchemaJson (string)          ? ? NodeParameterSchema JSON
|     IsEnabled (bool)       ?
|   IdPrefix (string)      ? ? "llm"
|     FullTypeName (string?)       ? ? Reflection type name
|     CreatedAt (DateTime) ?
|     UpdatedAt (DateTime)      ?
+--       ?
 ? Used by (many-to-many via JSON)
      ?
      +-- WorkflowDefinition.Nodes[].Type
```

### Indexes

```sql
-- Workflows
CREATE INDEX IX_Workflows_Name ON Workflows(Name);
CREATE INDEX IX_Workflows_CreatedAt ON Workflows(CreatedAt);

-- WorkflowInstances
CREATE INDEX IX_WorkflowInstances_WorkflowDefinitionId ON WorkflowInstances(WorkflowDefinitionId);
CREATE INDEX IX_WorkflowInstances_StartedAt ON WorkflowInstances(StartedAt);
CREATE INDEX IX_WorkflowInstances_Status ON WorkflowInstances(Status);

-- NodeTypes
CREATE UNIQUE INDEX IX_NodeTypes_NodeType ON NodeTypes(NodeType);
CREATE INDEX IX_NodeTypes_Category ON NodeTypes(Category);
```

---

## Design Patterns

### 1. Repository Pattern

```
+-- ? IWorkflowRepository  ? (interface)
+-- ? + GetAllAsync()       ?
| + GetByIdAsync(id)        ?
| + CreateAsync(workflow)        ?
| + UpdateAsync(workflow)        ?
| + DeleteAsync(id)              ?
| + SearchAsync(query)  ?
+--     ?
        ? implements
       ?
+-- ? SqliteWorkflowRepository   ? (implementation)
+-- ? - _context: DbContext        ?
| - _logger: ILogger    ?
+-- ? + GetAllAsync()     ?
|   - SELECT * FROM Workflows   ?
|      - Deserialize JSON       ?
+-- ```

**Benefits:**
- Testability (mock repositories)
- Separation of concerns
- Easy to swap implementations (JSON file ? SQLite ? PostgreSQL)

### 2. Unit of Work Pattern

```
+-- ? IUnitOfWork      ?
+-- ? + Workflows: IWorkflowRepo     ?
? + NodeTypes: INodeTypeRepo     ?
? + Instances: IInstanceRepo     ?
? + SaveChangesAsync()  ?
+--              ?
+-- ? UnitOfWork    ?
+-- ? - _context: DbContext          ?
+-- ? Transaction coordination:      ?
|   � Create workflow       ?
|   � Create nodes   ?
|   � Commit or rollback all     ?
+-- ```

### 3. Factory Pattern (Node Creation)

```
+-- ? INodeFactory      ?
+-- ? + CreateNode(definition, data) ?
+--              ?
+-- ? ReflectionNodeFactory   ?
+-- ? - Assembly scanning            ?
| - Type cache          ?
+-- ? CreateNode(definition):        ?
|   1. Look up type by name   ?
|   2. Activate via reflection ?
|   3. Inject parameters         ?
|   4. Return INode instance     ?
+-- ```

### 4. Strategy Pattern (Variable Resolver)

```
+-- ? IVariableResolver              ? (strategy interface)
+-- ? + Resolve(template, data)?
+--              ?
    ? implements
         +--           ? |         ?
+-- +-- +--
? TemplateVariable-     ? ? JsonPath-          ? ? Expression-      ?
? Resolver      ? ? VariableResolver? ? VariableResolver ?
? ({{key}})         ? ? ($.path.to.key)    ? ? (JS expressions) ?
+-- +-- +--
```

### 5. Decorator Pattern (Node Executor)

```
+-- ? INodeExecutor         ?
+-- ? + ExecuteAsync(node, data)     ?
+--      ?
        ? wraps
    ?
+-- ? RetryableNodeExecutor    ? (decorator)
+-- ? - _baseExecutor    ?
+-- ? ExecuteAsync():   ?
|   � Apply retry logic ?
|   � Apply timeout              ?
|   � Apply continue-on-error    ?
|   � Delegate to base           ?
+-- ```

### 6. Observer Pattern (Step Events)

```
+-- ? WorkflowDefinitionRunner       ?
+-- ? RunWithCallbackAsync(      ?
|   definition,           ?
|   data, ?
|   onStep: Func<Event, Task>    ? ? observer callback
| )       ?
+--              ?
         ? emits events
             ?
+-- ? NodeStepEvent              ?
+-- ? � EventType   ?
| � NodeId     ?
| � NodeName            ?
| � Timestamp           ?
| � Inputs / Outputs      ?
+--       ?
   ? consumed by
             ?
+-- ? Observer (Controller)      ?
+-- ? onStep = async (event) => {    ?
|   await WriteSSE(event);       ?
|   await LogToDatabase(event);  ?
| }          ?
+-- ```

### 7. Builder Pattern (Walk Configuration)

```
var config = new WalkConfiguration
{
    StartNodeId = startNode.Id,
  Data = workflowData,
    Context = context,
    NodeMap = nodeMap,
    Routing = routing,
    WorkflowDefinition = definition,
    OnStep = stepCallback
};

await _graphWalker.WalkAsync(config);
```

---

## Concurrency Model

### Thread Safety

```
+-- ? Singleton Services (Thread-Safe)    ?
+-- ? � IHttpClientProvider       ?
|   ? Connection pool (thread-safe)    ?
|         ?
? � INodeFactory    ?
|   ? Read-only type cache       ?
|          ?
? � IVariableResolver        ?
|   ? Stateless operation   ?
+-- 
+-- ? Scoped Services (Request Isolation)  ?
+-- ? � WorkflowGraphWalker         ?
|   ? One instance per request    ?
|      ?
? � WorkflowDbContext ?
|   ? EF Core change tracker isolated  ?
|        ?
? � Repositories ?
|   ? Per-request DbContext     ?
+-- ```

### Async/Await Flow

```csharp
// Controller (entry point)
public async Task<IActionResult> Run(Guid id)
{
    var workflow = await _repository.GetByIdAsync(id); // DB I/O
    
    var result = await _runner.RunAsync(workflow); // Workflow execution
    
    return Json(result);
}

// Runner (orchestrator)
public async Task<Result> RunAsync(WorkflowDefinition def)
{
    var walkResult = await _graphWalker.WalkAsync(config); // Graph walk
    
    return MapToResult(walkResult);
}

// Graph Walker (traversal)
public async Task<WalkResult> WalkAsync(WalkConfiguration config)
{
 while (hasMoreNodes)
    {
        var data = await _executor.ExecuteAsync(node, data); // Node exec
     currentNode = GetNextNode(data);
    }
    
    return result;
}

// Node Executor (with retry)
public async Task<WorkflowData> ExecuteAsync(INode node, WorkflowData data)
{
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
    try
        {
     return await node.ExecuteAsync(data, context); // Actual work
        }
      catch when (attempt < maxRetries)
 {
 await Task.Delay(retryDelay);
        }
    }
}
```

**Key Points:**
- No blocking calls (no `.Result` or `.Wait()`)
- Async all the way up the stack
- Cancellation token propagation
- ConfigureAwait(false) not needed in ASP.NET Core

### Cancellation Support

```csharp
// Cancellation flow
HttpContext.RequestAborted (CancellationToken)
    ?
WorkflowContext.CancellationToken
    ?
GraphWalker checks token in loop
    ?
Node.ExecuteAsync(data, context)
    ?
HttpClient.SendAsync(request, context.CancellationToken)
```

---

## Error Handling Architecture

### Exception Handling Hierarchy

```
+-- ? Level 1: Global Exception Handler (Middleware)         ?
+-- ? � Catches unhandled exceptions          ?
| � Maps to ProblemDetails  ?
| � Logs with correlation ID            ?
| � Returns HTTP 500/400/404/etc.     ?
+--            ? (fallback)
    ?
+-- ? Level 2: Controller Try/Catch      ?
+-- ? � Business logic exceptions      ?
| � Return custom error responses                  ?
| � Log context-specific errors            ?
+--    ? (fallback)
  ?
+-- ? Level 3: Workflow Execution Error Routing               ?
+-- ? � Node execution failures    ?
| � Route to error port or error node          ?
| � Continue workflow execution         ?
+--          ? (fallback)
          ?
+-- ? Level 4: Node Executor Retry Logic       ?
+-- ? � Transient failures (network, timeout)            ?
| � Retry with exponential backoff   ?
| � Log each attempt           ?
+--           ? (fallback)
            ?
+-- ? Level 5: Node Continue-on-Error     ?
+-- ? � Non-critical node failures          ?
| � Use fallback data   ?
| � Continue to next node ?
+-- ```

### Error Flow Example

```
Node Execution Fails
    ?
    Has ContinueOnError?
    |   YES ? Use fallback data, continue
    |   NO ?
    ?
    Has Retry Policy?
    |   YES ? Retry with backoff
    |   NO ?
    ?
    Has Node Error Port?
    |   YES ? Route to error port connection
    |   NO ?
    ?
    Has Workflow Error Node?
    |   YES ? Route to workflow error node
    |   NO ?
    ?
    Fail Workflow
        Return WorkflowRunResult.Failure()
            Controller returns HTTP 422
```

---

## Performance Optimizations

### 1. Database Connection Pooling

```csharp
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(connectionString)
      .EnableSensitiveDataLogging(isDevelopment)
           .EnableDetailedErrors(isDevelopment));
```

SQLite connection pooling is handled by EF Core.

### 2. HTTP Client Pooling

```csharp
public class PooledHttpClientProvider : IHttpClientProvider
{
    private readonly ObjectPool<HttpClient> _pool;
    
    public PooledHttpClientProvider(TimeSpan clientLifetime)
    {
        _pool = ObjectPool.Create(new HttpClientPooledObjectPolicy
        {
            Lifetime = clientLifetime
        });
    }
    
    public HttpClient GetClient() => _pool.Get();
}
```

### 3. Async Streaming (SSE)

```csharp
// Server-Sent Events for real-time updates without polling
Response.Headers["Content-Type"] = "text/event-stream";
Response.Headers["Cache-Control"] = "no-cache";

await Response.WriteAsync($"event: node_complete\ndata: {json}\n\n");
await Response.Body.FlushAsync();
```

**Benefits:**
- No HTTP polling overhead
- Real-time updates
- Low latency

### 4. Scoped Logging

```csharp
using var scope = _logger.BeginWorkflowScope(
    workflowId,
    workflowName,
    nodeCount);
```

Reduces log noise while maintaining correlation.

---

## Security Architecture

### Defense in Depth

```
+-- ? Layer 1: Transport Security ?
+-- ? � HTTPS enforcement     ?
? � HSTS headers            ?
+--  ?
+-- ? Layer 2: Authentication & Authorization (Planned)  ?
+-- ? � JWT tokens     ?
? � Role-based access control        ?
+--         ?
+-- ? Layer 3: Input Validation ?
+-- ? � Anti-forgery tokens (CSRF)            ?
? � Model binding validation ?
? � SQL injection prevention (EF parameterization)      ?
+--               ?
+-- ? Layer 4: Business Logic Validation ?
+-- ? � Workflow structure validation      ?
? � Node parameter validation           ?
? � Execution quotas/limits            ?
+--            ?
+-- ? Layer 5: Error Handling?
+-- ? � Exception details filtered in production  ?
? � Correlation IDs for tracing            ?
? � Structured logging (no sensitive data)                ?
+-- ```

---

## Deployment Architecture

### Single Server Deployment

```
+-- ? Server (Windows/Linux)       ?
+-- |         ?
|  +--     ?
|  ? IIS / Nginx (Reverse Proxy)         |     ?
|  ? � SSL termination   |     ?
|  ? � Static file serving          |     ?
|  ? � Request routing         |     ?
|  +--   ?
|       |        ?
|  +--     ?
|  ? Kestrel (ASP.NET Core)            |     ?
|  ? � Dynamic content         |     ?
|  ? � API endpoints    |     ?
|  ? � SSE streaming    |     ?
|  +--  ?
|      |     ?
|  +--     ?
|  ? SQLite Database    |     ?
|  ? /var/data/workflows.db         |     ?
|  +--
|      ?
+-- ```

### Docker Deployment

```
+-- ? Docker Host         ?
+-- ? ?
?  +--     ?
?  ? Container: twf-ai-framework-web        |     ?
?  |      |     ?
?  ?  � ASP.NET Core Runtime   |     ?
?  ?  � Application binaries       |     ?
?  ?  � wwwroot static files    |     ?
?  |     |     ?
?  ?  Volume mount: /app/data ? /host/data       ?  ?
?  ?  Port: 8080:80, 8443:443        |     ?
?  +--     ?
|   ?
?  +--     ?
?  ? Host Volume: /host/data              |     ?
?  ?  � workflows.db (persistent)   |     ?
?  +--     ?
?  ?
+-- ```

---

## Conclusion

This architecture document provides a comprehensive view of the Twf.Flow Web Application's internal structure, design patterns, and execution flows. The system is designed for:

- **Scalability**: Async/await, connection pooling, scoped services
- **Maintainability**: Clean architecture, SOLID principles, separation of concerns
- **Extensibility**: Plugin-based node system, repository pattern, strategy pattern
- **Observability**: Structured logging, correlation IDs, performance metrics
- **Reliability**: Multi-level error handling, retry logic, graceful degradation

For implementation details, see the main [DOCUMENTATION.md](./DOCUMENTATION.md) file.

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Framework Version:** .NET 10.0
