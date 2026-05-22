# Twf.Flow - Web Application Documentation

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Core Components](#core-components)
- [Features](#features)
- [API Reference](#api-reference)
- [Database Schema](#database-schema)
- [Workflow Execution](#workflow-execution)
- [Frontend Architecture](#frontend-architecture)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Performance](#performance)
- [Security](#security)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)

---

## Overview

The Twf.Flow Web Application is a visual workflow designer and execution engine built on ASP.NET Core MVC. It provides a drag-and-drop interface for creating, managing, and executing AI-powered workflows using a node-based visual programming paradigm.

### Key Features
- **Visual Workflow Designer**: Drag-and-drop interface with real-time canvas manipulation
- **Workflow Execution Engine**: Robust execution with retry logic, timeout handling, and error recovery
- **Node Type Management**: Dynamic node registration and schema-based configuration
- **Real-time Streaming**: Server-Sent Events (SSE) for live execution monitoring
- **Persistent Storage**: SQLite database with Entity Framework Core
- **Sub-workflow Support**: Hierarchical workflow composition
- **Loop & Branch Nodes**: Advanced control flow structures
- **Structured Logging**: Comprehensive observability with correlation IDs
- **Error Recovery**: Global exception handling and workflow-level error routing

### Technology Stack
- **.NET 10.0**: Latest framework with C# 13
- **ASP.NET Core MVC**: Server-side rendering with Razor Pages
- **Entity Framework Core 10.0**: Database ORM with SQLite provider
- **Bootstrap 5**: Responsive UI framework
- **Vanilla JavaScript**: Custom workflow designer (no framework dependencies)
- **Server-Sent Events**: Real-time execution streaming

---

## Architecture

### High-Level Architecture

```
+-- ?  Web Application     ?
+-- |         ?
|  +--  +--  +--      ?
|  ? Controllers  ?  ?  Services    ?  ? Repositories ? ?
|  |   ?  |              ?  |     |      ?
|  ? � Workflow   +-- � Runner     +-- � Workflow   |      ?
|  ? � Node     ?  ? � GraphWalker?  ? � NodeType   |      ?
|  ? � Runner     ?  ? � Executor   ?  ? � Instance   ? ?
|  +--  ? � Factory    ?  +--      ?
|      ? � Resolver   |     |       ?
|        +--         |      ?
|  |     ?
|  +--  +--        |    ?
|  ?  Middleware  ?  |    Models    |     |              ?
|  |        ?  ?|        ?  ?
|  ? � Exception  ?  ? � Workflow   |   |           ?
|  ? � Correlation?  ? � Node       |    |    ?
|  |   ID     ?  ? � Instance   ?  +--     ?
|  +--  +--  ?  SQLite DB   |     ?
|   |         |     ?
|  +-- ? � Workflows  |     ?
|  |      Frontend (Views/JS)          ? ? � NodeTypes  |     ?
|  |            ? ? � Instances  |     ?
|  ? � Designer UI    ? +--     ?
|  ? � Canvas Rendering            ? ?
|  ? � Node Palette        |           ?
|  ? � Property Editor       
|  +--    ?
+-- ```

### Service Layer Architecture

The application follows a **clean architecture** pattern with clear separation of concerns:

#### 1. **Controllers** (Presentation Layer)
- `WorkflowController`: Workflow CRUD operations and designer endpoints
- `WorkflowRunnerController`: Workflow execution and streaming
- `NodeController`: Node type management
- `HomeController`: Landing pages

#### 2. **Services** (Business Logic Layer)
- **WorkflowDefinitionRunner**: Orchestrates workflow execution lifecycle
- **IWorkflowGraphWalker**: Graph traversal and node execution coordination
- **INodeExecutor**: Individual node execution with retry/timeout
- **INodeFactory**: Dynamic node instantiation from definitions
- **IVariableResolver**: Template variable substitution (`{{variable}}`)
- **INodeSchemaProvider**: Node parameter schema management

#### 3. **Repositories** (Data Access Layer)
- **IWorkflowRepository**: Workflow persistence
- **INodeTypeRepository**: Node type persistence
- **IWorkflowInstanceRepository**: Execution history tracking
- **IUnitOfWork**: Transaction management

#### 4. **Middleware**
- **GlobalExceptionHandler**: Centralized error handling with ProblemDetails
- **CorrelationIdMiddleware**: Request tracing and observability

---

## Getting Started

### Prerequisites
```bash
# Required
.NET 10.0 SDK

# Recommended
Visual Studio 2025 or Visual Studio Code
SQLite Browser (for database inspection)
Modern web browser (Chrome, Firefox, Edge)
```

### Installation & Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/techwayfit/Twf_AI_Framework.git
   cd Twf_AI_Framework/source/web
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure application settings**
   
   Edit `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*",
     "UseDatabase": true,
  "ConnectionStrings": {
       "WorkflowDb": "Data Source=workflows.db"
     }
   }
   ```

4. **Run the application**
```bash
   dotnet run
   ```

5. **Access the application**
   - Navigate to: `https://localhost:5001`
   - The database will be created automatically on first run
   - Sample node types will be seeded

### First Workflow

1. Click **"Create New Workflow"** from the home page
2. Enter workflow name and description
3. Click **"Create"** to open the designer
4. Drag a **Start Node** from the palette onto the canvas
5. Add additional nodes (e.g., LLM, HTTP Request)
6. Connect nodes by dragging from output ports to input ports
7. Add an **End Node** to mark completion
8. Click **"Save"** to persist the workflow
9. Navigate to **"Run"** to execute the workflow

---

## Core Components

### 1. WorkflowDefinition Model

Represents the complete structure of a workflow.

```csharp
public class WorkflowDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public List<NodeDefinition> Nodes { get; set; }
    public List<ConnectionDefinition> Connections { get; set; }
 public Dictionary<string, object> Variables { get; set; }
    
    // Error handling
    public Guid? ErrorNodeId { get; set; }
    
    // Sub-workflows
    public List<ChildWorkflowDefinition> SubWorkflows { get; set; }
    
    // Metadata
    public WorkflowMetadata Metadata { get; set; }
}
```

#### NodeDefinition
```csharp
public class NodeDefinition
{
    public Guid Id { get; set; }
    public string NodeId { get; set; }  // Human-readable ID (e.g., "llm001")
    public string Name { get; set; }
    public string Type { get; set; }       // Node type (e.g., "LlmNode")
    public string Category { get; set; }   // Category (e.g., "AI", "Data")
    
    public Dictionary<string, object?> Parameters { get; set; }
    public NodePosition Position { get; set; }
    public string? Color { get; set; }
    
    // Execution configuration
    public NodeExecutionOptions? ExecutionOptions { get; set; }
    
  // Sub-workflow (for Loop, Parallel, Branch nodes)
    public SubWorkflowDefinition? SubWorkflow { get; set; }
    public bool IsExpanded { get; set; }
}
```

#### NodeExecutionOptions
```csharp
public class NodeExecutionOptions
{
    public int MaxRetries { get; set; } = 0;
    public int RetryDelayMs { get; set; } = 1000;
    public int? TimeoutMs { get; set; }
  public bool ContinueOnError { get; set; } = false;
    public string? RunCondition { get; set; }
    public Dictionary<string, object?>? FallbackData { get; set; }
}
```

### 2. WorkflowDefinitionRunner

The main orchestrator for workflow execution.

```csharp
public sealed class WorkflowDefinitionRunner
{
    // Execute workflow with real-time callback for each step
 public Task<WorkflowRunResult> RunWithCallbackAsync(
        WorkflowDefinition definition,
        WorkflowData? initialData,
        Func<NodeStepEvent, Task> onStep)
    
    // Execute workflow and return final result
    public Task<WorkflowRunResult> RunAsync(
        WorkflowDefinition definition,
        WorkflowData? initialData = null)
}
```

**Workflow Execution Lifecycle:**
1. Validate workflow has a Start node
2. Initialize `WorkflowContext` and `WorkflowData`
3. Seed workflow-level variables
4. Build routing table from connections
5. Delegate to `IWorkflowGraphWalker` for graph traversal
6. Log performance metrics
7. Return `WorkflowRunResult`

### 3. WorkflowGraphWalker

Handles graph traversal and node execution.

```csharp
public class WorkflowGraphWalker : IWorkflowGraphWalker
{
    public Task<WalkResult> WalkAsync(WalkConfiguration config)
}
```

**Supported Node Types:**
- **Structural Nodes**: StartNode, EndNode, ErrorNode
- **Control Flow**: BranchNode, LoopNode, SubWorkflowNode
- **Regular Nodes**: All other node types (LLM, HTTP, etc.)

**Execution Flow:**
```
Start ? Node Execution ? Route Selection ? Next Node ? ... ? End
    ? (on error)
 Error Handler (if configured)
```

### 4. Node Factory & Executor

#### INodeFactory
Dynamically creates node instances from definitions.

```csharp
public interface INodeFactory
{
    INode? CreateNode(NodeDefinition definition, WorkflowData data);
}
```

#### INodeExecutor
Executes individual nodes with retry, timeout, and error handling.

```csharp
public interface INodeExecutor
{
  Task<WorkflowData> ExecuteAsync(
        INode node,
  WorkflowData data,
        WorkflowContext context,
        NodeOptions options);
}
```

**RetryableNodeExecutor Features:**
- Exponential backoff retry logic
- Timeout enforcement
- Continue-on-error with fallback data
- Structured logging of attempts

### 5. Variable Resolver

Resolves template variables in node parameters.

```csharp
public interface IVariableResolver
{
    object? Resolve(string template, WorkflowData data);
}
```

**Template Syntax:**
- Simple: `{{variable_name}}`
- Nested: `{{nodeId.output_key}}`
- Fallback: `{{variable "default"}}`

---

## Features

### 1. Visual Workflow Designer

#### Canvas Features
- **Pan & Zoom**: Mouse drag to pan, scroll to zoom
- **Node Dragging**: Click and drag nodes to reposition
- **Connection Drawing**: Bezier curves with source/target highlighting
- **Multi-selection**: Ctrl+Click to select multiple nodes
- **Delete**: Select and press Delete key

#### Node Palette
- **Categorized Nodes**: AI, Data, Control, IO
- **Drag-to-Add**: Drag nodes from palette onto canvas
- **Search/Filter**: Find nodes by name or category

#### Properties Panel
- **Node Configuration**: Edit name, parameters, execution options
- **Dynamic Schema**: Parameter fields generated from node schema
- **Validation**: Real-time validation of required fields

### 2. Workflow Execution

#### Standard Execution (POST /Workflow/Run/{id})
Returns complete result as JSON when execution finishes.

**Request:**
```json
POST /Workflow/Run/123e4567-e89b-12d3-a456-426614174000
Content-Type: application/json

{
  "initialData": {
    "input_text": "Hello, world!"
  }
}
```

**Response (Success):**
```json
{
  "result": {
    "isSuccess": true,
    "workflowName": "My Workflow",
    "outputData": {
 "final_result": "Processed output",
      "llm001.response": "LLM response text"
    },
    "errorMessage": null,
    "failedNodeName": null
  },
  "instanceId": "456e7890-e89b-12d3-a456-426614174001"
}
```

**Response (Failure):**
```json
{
  "result": {
    "isSuccess": false,
  "workflowName": "My Workflow",
    "outputData": {},
    "errorMessage": "HTTP request failed: 404 Not Found",
    "failedNodeName": "HTTP Fetch"
  },
  "instanceId": "456e7890-e89b-12d3-a456-426614174001"
}
```

#### Streaming Execution (POST /Workflow/RunStream/{id})
Streams execution events in real-time via Server-Sent Events (SSE).

**Request:**
```javascript
const eventSource = new EventSource('/Workflow/RunStream/123e4567-...');

eventSource.addEventListener('node_start', (e) => {
  const data = JSON.parse(e.data);
  console.log(`Started: ${data.nodeName}`);
});

eventSource.addEventListener('node_complete', (e) => {
  const data = JSON.parse(e.data);
  console.log(`Completed: ${data.nodeName}`, data.outputs);
});

eventSource.addEventListener('workflow_done', (e) => {
  const data = JSON.parse(e.data);
  console.log('Workflow completed!', data.result);
  eventSource.close();
});
```

**Event Types:**
- `node_start`: Node execution begins
- `node_complete`: Node execution succeeds
- `node_error`: Node execution fails (but workflow continues)
- `loop_iteration_start`: Loop iteration begins
- `workflow_done`: Workflow completes successfully
- `workflow_error`: Workflow fails

### 3. Sub-Workflows

Sub-workflows enable hierarchical workflow composition.

**Types:**
1. **Child Workflows**: Reusable workflows stored in `SubWorkflows` collection
2. **Container Sub-workflows**: Inline sub-workflows in Loop/Branch/Parallel nodes

**SubWorkflowNode:**
```json
{
  "type": "SubWorkflowNode",
  "parameters": {
    "subWorkflowId": "789e0123-e89b-12d3-a456-426614174002"
  }
}
```

**Execution:**
- Clones parent workflow data
- Executes child workflow in isolation
- Returns updated data to parent
- Supports `success` and `error` output ports

### 4. Loop Node

Iterates over a collection, executing a sub-workflow for each item.

**Configuration:**
```json
{
  "type": "LoopNode",
  "parameters": {
    "itemsKey": "{{users}}",        // Collection to iterate
    "outputKey": "results",// Output key for results
    "loopItemKey": "__item__",       // Current item variable name
    "maxIterations": 100   // Safety limit
  }
}
```

**Execution:**
1. Retrieves collection from `itemsKey`
2. For each item:
   - Sets `__item__` and `__loop_index__` in workflow data
   - Executes `body` sub-workflow
   - Collects results
3. Stores results in `outputKey`
4. Routes to `output` port

**Output:**
```json
{
  "results": [
    { "processed_user": "user1_data" },
    { "processed_user": "user2_data" }
  ],
  "loop_iteration_count": 2
}
```

### 5. Branch Node

Conditional routing based on expression evaluation.

**Configuration:**
```json
{
  "type": "BranchNode",
  "parameters": {
    "condition": "{{score}} > 0.8",
    "truePort": "high_confidence",
    "falsePort": "low_confidence"
  }
}
```

### 6. Node Type Management

#### Database-Driven Node Registry
Node types are stored in the `NodeTypes` table with full schema definitions.

**NodeTypeEntity:**
```csharp
public class NodeTypeEntity
{
    public int Id { get; set; }
    public string NodeType { get; set; }      // "LlmNode"
    public string Name { get; set; }     // "LLM"
    public string Category { get; set; }      // "AI"
    public string Description { get; set; }
    public string Color { get; set; }       // "#4A90E2"
    public string Icon { get; set; }          // "bi-cpu"
    public string SchemaJson { get; set; }    // NodeParameterSchema JSON
    public bool IsEnabled { get; set; }
    public string IdPrefix { get; set; }      // "llm"
    public string? FullTypeName { get; set; } // For reflection
}
```

#### CRUD Operations
- **GET /Node**: List all node types (with category filter)
- **GET /Node/Create**: Create new node type
- **POST /Node/Create**: Save new node type
- **GET /Node/Edit/{id}**: Edit node type
- **POST /Node/Edit/{id}**: Update node type
- **POST /Node/Delete/{id}**: Delete node type
- **POST /Node/ToggleEnabled/{id}**: Enable/disable node type

#### Schema Definition
```json
{
  "nodeType": "LlmNode",
  "displayName": "LLM",
  "description": "Call a Large Language Model",
  "category": "AI",
  "parameters": [
    {
      "name": "provider",
    "displayName": "Provider",
      "type": "string",
    "required": true,
      "defaultValue": "openai",
      "description": "LLM provider",
      "options": ["openai", "anthropic", "azure"]
    },
    {
    "name": "model",
      "displayName": "Model",
      "type": "string",
"required": true,
      "defaultValue": "gpt-4",
      "description": "Model identifier"
    },
    {
   "name": "temperature",
      "displayName": "Temperature",
      "type": "number",
      "required": false,
      "defaultValue": 0.7,
    "min": 0.0,
    "max": 2.0
    }
  ],
  "inputs": [
    { "key": "prompt", "displayName": "Prompt", "required": true }
  ],
  "outputs": [
  { "key": "response", "displayName": "Response" },
    { "key": "usage", "displayName": "Token Usage" }
  ]
}
```

---

## API Reference

### Workflow Management API

#### List Workflows
```http
GET /Workflow?search={query}
```
Returns HTML view with workflow list.

#### Get Workflow JSON
```http
GET /Workflow/GetWorkflow/{id}
```
**Response:**
```json
{
  "id": "guid",
  "name": "Workflow Name",
  "nodes": [...],
  "connections": [...],
  "variables": {}
}
```

#### Save Workflow
```http
POST /Workflow/SaveWorkflow
Content-Type: application/json

{
  "id": "guid",
  "name": "Updated Name",
  "nodes": [...],
  "connections": [...]
}
```
**Response:**
```json
{
  "success": true,
  "id": "guid"
}
```

#### Get Available Nodes
```http
GET /Workflow/GetAvailableNodes
```
**Response:**
```json
[
  {
    "type": "LlmNode",
    "category": "AI",
    "name": "LLM",
    "description": "Call a Large Language Model",
    "color": "#4A90E2",
    "icon": "bi-cpu",
    "idPrefix": "llm",
    "fullTypeName": "Twf.Flow.Nodes.AI.LlmNode, Twf.Flow.Core",
    "defaultParams": {
      "provider": "openai",
      "model": "gpt-4"
    }
  }
]
```

#### Get Node Schema
```http
GET /Workflow/GetNodeSchema/{nodeType}
```
**Response:**
```json
{
  "nodeType": "LlmNode",
  "parameters": [...],
  "inputs": [...],
  "outputs": [...]
}
```

### Workflow Execution API

#### Execute Workflow (Blocking)
```http
POST /Workflow/Run/{id}
Content-Type: application/json

{
  "initialData": {
    "key": "value"
  }
}
```

#### Execute Workflow (Streaming)
```http
POST /Workflow/RunStream/{id}
Content-Type: application/json

{
  "initialData": {
    "key": "value"
  }
}
```
Returns `text/event-stream` with SSE events.

#### View Workflow Runs
```http
GET /Workflow/Runs/{id}
```
Returns HTML view with execution history.

#### View Run Details
```http
GET /Workflow/RunDetail/{instanceId}
```
Returns HTML view with detailed execution log.

---

## Database Schema

### Tables

#### Workflows
```sql
CREATE TABLE Workflows (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    JsonData TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE INDEX IX_Workflows_Name ON Workflows(Name);
CREATE INDEX IX_Workflows_CreatedAt ON Workflows(CreatedAt);
```

#### NodeTypes
```sql
CREATE TABLE NodeTypes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NodeType TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    Description TEXT,
    Color TEXT,
    Icon TEXT,
    SchemaJson TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IdPrefix TEXT DEFAULT 'node',
    FullTypeName TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IX_NodeTypes_NodeType ON NodeTypes(NodeType);
CREATE INDEX IX_NodeTypes_Category ON NodeTypes(Category);
```

#### WorkflowInstances
```sql
CREATE TABLE WorkflowInstances (
    Id TEXT PRIMARY KEY,
    WorkflowDefinitionId TEXT NOT NULL,
    WorkflowName TEXT NOT NULL,
Status TEXT NOT NULL,
    StartedAt TEXT NOT NULL,
    CompletedAt TEXT,
    JsonData TEXT NOT NULL
);

CREATE INDEX IX_WorkflowInstances_WorkflowDefinitionId ON WorkflowInstances(WorkflowDefinitionId);
CREATE INDEX IX_WorkflowInstances_StartedAt ON WorkflowInstances(StartedAt);
CREATE INDEX IX_WorkflowInstances_Status ON WorkflowInstances(Status);
```

### Entity Relationships

```
Workflows (1) +-- (N) WorkflowInstances
           (tracks execution history)

NodeTypes (N) +-- (N) Workflows
          (through node definitions in JSON)
```

---

## Workflow Execution

### Execution Pipeline

```
User Request
    ?
WorkflowRunnerController
    ?
WorkflowDefinitionRunner.RunWithCallbackAsync()
    ?
WorkflowGraphWalker.WalkAsync()
    ?
For each node:
    +-- INodeFactory.CreateNode()
    +-- IVariableResolver.Resolve()
    +-- INodeExecutor.ExecuteAsync()
    |   +-- Retry logic
    |   +-- Timeout enforcement
    |   +-- Error handling
    +-- Route to next node
    ?
Return WorkflowRunResult
```

### Step Event Lifecycle

Each node execution emits events:

1. **node_start**
   ```json
   {
     "eventType": "node_start",
"nodeId": "guid",
     "nodeName": "My Node",
     "nodeType": "LlmNode",
     "timestamp": "2024-01-01T12:00:00Z",
 "inputs": { "prompt": "Hello" }
   }
   ```

2. **node_complete**
   ```json
   {
     "eventType": "node_complete",
     "nodeId": "guid",
     "nodeName": "My Node",
     "nodeType": "LlmNode",
     "timestamp": "2024-01-01T12:00:02Z",
     "outputs": { "response": "Hi there!" }
   }
 ```

3. **node_error** (if failure)
   ```json
   {
     "eventType": "node_error",
     "nodeId": "guid",
     "nodeName": "My Node",
     "nodeType": "LlmNode",
     "timestamp": "2024-01-01T12:00:02Z",
     "errorMessage": "API timeout"
   }
   ```

### Error Handling Strategies

#### 1. Node-Level Error Handling
```csharp
// Retry with exponential backoff
{
  "executionOptions": {
    "maxRetries": 3,
    "retryDelayMs": 1000
  }
}

// Timeout enforcement
{
  "executionOptions": {
    "timeoutMs": 30000
  }
}

// Continue on error with fallback
{
  "executionOptions": {
    "continueOnError": true,
    "fallbackData": {
      "response": "default_value"
    }
  }
}
```

#### 2. Workflow-Level Error Routing
```json
{
  "errorNodeId": "error-handler-guid"
}
```
When any node fails without its own error handler, workflow routes to this node.

#### 3. Connection-Level Error Routing
```json
{
  "connections": [
    {
      "sourceNodeId": "node-guid",
      "sourcePort": "error",
      "targetNodeId": "error-handler-guid"
    }
  ]
}
```

---

## Frontend Architecture

### Designer Application Structure

```
wwwroot/
+-- js/
|   +-- designer-app/
|   |   +-- designer.js       # Main designer logic
|   |   +-- designer.css      # Designer styles
|   +-- site.js         # Global site JS
+-- css/
    +-- site.css          # Global styles
```

### Key Frontend Components

#### 1. Canvas Rendering
```javascript
class WorkflowCanvas {
  constructor(containerId) {
    this.canvas = document.getElementById(containerId);
    this.ctx = this.canvas.getContext('2d');
    this.nodes = [];
    this.connections = [];
    this.zoom = 1.0;
this.panX = 0;
    this.panY = 0;
  }
  
  render() {
    // Clear canvas
// Apply zoom & pan transform
    // Draw connections (bezier curves)
    // Draw nodes (rounded rectangles)
    // Draw ports (circles)
  }
}
```

#### 2. Node Palette
```javascript
class NodePalette {
  async loadAvailableNodes() {
    const response = await fetch('/Workflow/GetAvailableNodes');
    const nodes = await response.json();
    this.renderPalette(nodes);
  }
  
  renderPalette(nodes) {
    // Group by category
    // Create draggable node items
    // Attach drag event handlers
  }
}
```

#### 3. Property Editor
```javascript
class PropertyEditor {
  async loadNodeSchema(nodeType) {
    const response = await fetch(`/Workflow/GetNodeSchema/${nodeType}`);
    const schema = await response.json();
    this.renderProperties(schema);
  }
  
  renderProperties(schema) {
    // Dynamically create input fields based on parameter types
    // Bind change events to update node definition
  }
}
```

#### 4. Workflow Persistence
```javascript
class WorkflowManager {
  async saveWorkflow() {
    const definition = this.serializeWorkflow();
    const response = await fetch('/Workflow/SaveWorkflow', {
    method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(definition)
    });
    const result = await response.json();
    return result.success;
  }
  
  serializeWorkflow() {
    return {
 id: this.workflowId,
      name: this.workflowName,
      nodes: this.canvas.nodes.map(n => ({
      id: n.id,
        nodeId: n.nodeId,
     name: n.name,
        type: n.type,
        parameters: n.parameters,
      position: { x: n.x, y: n.y }
      })),
      connections: this.canvas.connections.map(c => ({
      id: c.id,
sourceNodeId: c.sourceNodeId,
        sourcePort: c.sourcePort,
        targetNodeId: c.targetNodeId,
targetPort: c.targetPort
      }))
    };
  }
}
```

---

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "UseDatabase": true,
  "WorkflowDataDirectory": "workflows",
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=workflows.db"
  }
}
```

### Environment-Specific Configuration

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=workflows-dev.db"
  }
}
```

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=/var/data/workflows.db"
  }
}
```

### Dependency Injection Configuration

From `Program.cs`:

```csharp
// Logging
builder.Logging.AddJsonConsole(options => {
options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
});

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// HTTP client pooling
builder.Services.AddSingleton<IHttpClientProvider, PooledHttpClientProvider>();

// Workflow services
builder.Services.AddSingleton<IVariableResolver, TemplateVariableResolver>();
builder.Services.AddSingleton<INodeFactory, ReflectionNodeFactory>();
builder.Services.AddSingleton<INodeSchemaProvider, ReflectionNodeSchemaProvider>();
builder.Services.AddScoped<INodeExecutor, RetryableNodeExecutor>();
builder.Services.AddScoped<IWorkflowGraphWalker, WorkflowGraphWalker>();
builder.Services.AddScoped<WorkflowDefinitionRunner>();

// Database
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IWorkflowRepository, SqliteWorkflowRepository>();
builder.Services.AddScoped<INodeTypeRepository, SqliteNodeTypeRepository>();
builder.Services.AddScoped<IWorkflowInstanceRepository, SqliteWorkflowInstanceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Error Handling

### Global Exception Handling

The `GlobalExceptionHandler` middleware provides centralized error handling:

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
      Exception exception,
        CancellationToken cancellationToken)
    {
        // Map exception types to appropriate HTTP status codes
   // Return ProblemDetails JSON
  // Log with correlation ID
    }
}
```

### Exception Type Mapping

| Exception Type | HTTP Status | Description |
|----------------|-------------|-------------|
| `ArgumentException` | 400 Bad Request | Invalid parameters |
| `KeyNotFoundException` | 404 Not Found | Resource not found |
| `InvalidOperationException` | 422 Unprocessable Entity | Business logic error |
| `TimeoutException` | 504 Gateway Timeout | Operation timeout |
| `UnauthorizedAccessException` | 403 Forbidden | Access denied |
| Others | 500 Internal Server Error | Unexpected errors |

### ProblemDetails Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred",
  "instance": "/Workflow/Run/123",
  "correlationId": "abc-123-def",
  "timestamp": "2024-01-01T12:00:00Z",
  "exceptionType": "NullReferenceException"
}
```

### Correlation ID Middleware

Tracks requests across distributed systems:

```csharp
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"]
            .FirstOrDefault() Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        await _next(context);
    }
}
```

---

## Performance

### Optimization Strategies

#### 1. HTTP Client Pooling
```csharp
builder.Services.AddSingleton<IHttpClientProvider>(sp =>
{
    return new PooledHttpClientProvider(
        clientLifetime: TimeSpan.FromMinutes(2),
logger: logger);
});
```

#### 2. Database Indexes
```sql
CREATE INDEX IX_Workflows_Name ON Workflows(Name);
CREATE INDEX IX_WorkflowInstances_WorkflowDefinitionId ON WorkflowInstances(WorkflowDefinitionId);
CREATE INDEX IX_WorkflowInstances_Status ON WorkflowInstances(Status);
```

#### 3. Async/Await Throughout
All I/O operations use async patterns to maximize throughput.

#### 4. Scoped Logging
Reduces log verbosity while maintaining observability:
```csharp
using var workflowScope = _logger.BeginWorkflowScope(
    definition.Id,
    definition.Name,
    definition.Nodes.Count);
```

### Performance Metrics

Logged automatically after each workflow execution:

```json
{
  "metric_name": "workflow_execution_duration",
  "value": 2345.67,
  "unit": "ms",
  "workflow_id": "guid",
  "workflow_name": "My Workflow",
  "node_count": 15,
  "success": true
}
```

---

## Security

### Best Practices

#### 1. Anti-Forgery Tokens
All state-changing POST requests require CSRF tokens:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(WorkflowDefinition workflow)
```

#### 2. Input Validation
- Model binding validation
- Schema-based parameter validation
- SQL injection prevention via EF Core parameterization

#### 3. Exception Details Filtering
```csharp
Detail = _environment.IsDevelopment() 
    ? exception.Message 
    : "An unexpected error occurred"
```

#### 4. HTTPS Enforcement
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

### Future Security Enhancements
- [ ] Authentication & Authorization (Identity Server)
- [ ] API key management for node types
- [ ] Workflow execution quotas/rate limiting
- [ ] Audit logging
- [ ] Encrypted secrets management

---

## Deployment

### Development Deployment

```bash
cd source/web
dotnet run
```

### Production Deployment (IIS)

1. **Publish application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configure IIS:**
   - Install ASP.NET Core Runtime 10.0
   - Create application pool (.NET CLR Version: No Managed Code)
   - Set identity to ApplicationPoolIdentity
   - Point to publish directory

3. **Configure web.config:**
```xml
   <configuration>
     <system.webServer>
       <handlers>
  <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" />
     </handlers>
  <aspNetCore processPath="dotnet"
     arguments=".\twf_ai_framework.web.dll"
     stdoutLogEnabled="true"
                stdoutLogFile=".\logs\stdout" />
     </system.webServer>
   </configuration>
   ```

### Docker Deployment

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["source/web/twf_ai_framework.web.csproj", "source/web/"]
COPY ["source/core/twf_ai_framework.csproj", "source/core/"]
RUN dotnet restore "source/web/twf_ai_framework.web.csproj"
COPY . .
WORKDIR "/src/source/web"
RUN dotnet build "twf_ai_framework.web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "twf_ai_framework.web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "twf_ai_framework.web.dll"]
```

**Build & Run:**
```bash
docker build -t twf-ai-framework-web .
docker run -d -p 8080:80 -p 8443:443 \
  -v /path/to/data:/app/data \
  twf-ai-framework-web
```

---

## Troubleshooting

### Common Issues

#### 1. Database Migration Fails
**Symptom:** Application crashes on startup with migration error.

**Solution:**
```bash
# Delete existing database
rm workflows.db

# Restart application to recreate
dotnet run
```

#### 2. Node Type Not Found
**Symptom:** "Unknown node type 'XxxNode' � skipping"

**Solution:**
- Verify node type is registered in `NodeTypes` table
- Check `IsEnabled = true`
- Verify `FullTypeName` matches actual .NET type
- Rebuild solution to ensure type is available

#### 3. Workflow Execution Hangs
**Symptom:** Workflow never completes or timeout.

**Solution:**
- Check for infinite loops (max 500 steps enforced)
- Verify all nodes have outgoing connections
- Check node timeout settings
- Review logs for stuck node

#### 4. Designer UI Not Loading
**Symptom:** Blank canvas or JavaScript errors.

**Solution:**
- Check browser console for JS errors
- Verify `/Workflow/GetAvailableNodes` returns data
- Clear browser cache
- Check network tab for failed API calls

#### 5. Connection String Error
**Symptom:** "Unable to open database file"

**Solution:**
- Verify file path is writable
- Check permissions on database directory
- Use absolute path in connection string
- Ensure SQLite provider is installed

### Logging & Diagnostics

#### Enable Detailed Logging
```json
{
  "Logging": {
    "LogLevel": {
  "Default": "Debug",
      "Twf.Flow.Web": "Trace"
    }
  }
}
```

#### View Structured Logs
```bash
dotnet run 2>&1 | jq -r 'select(.EventId.Name != null)'
```

#### Database Inspection
```bash
sqlite3 workflows.db
.schema
SELECT * FROM Workflows;
SELECT * FROM NodeTypes;
SELECT * FROM WorkflowInstances ORDER BY StartedAt DESC LIMIT 10;
```

---

## Appendix

### Glossary

- **Workflow**: A directed graph of nodes connected by edges, representing a computational process
- **Node**: An atomic unit of work (e.g., API call, data transformation)
- **Connection**: A directed edge linking two nodes, defining data flow
- **Port**: An input or output endpoint on a node
- **Sub-workflow**: A nested workflow executed within a parent workflow
- **WorkflowData**: Key-value dictionary holding runtime state
- **WorkflowContext**: Execution context with cancellation, logging, and state
- **Node Schema**: Metadata defining node parameters, inputs, and outputs

### Keyboard Shortcuts (Designer)

| Shortcut | Action |
|----------|--------|
| Delete | Delete selected node/connection |
| Escape | Deselect all |
| Ctrl+S | Save workflow |
| Ctrl+Z | Undo (planned) |
| Ctrl+Y | Redo (planned) |

### Browser Compatibility

| Browser | Version | Status |
|---------|---------|--------|
| Chrome | 90+ | ? Fully supported |
| Firefox | 88+ | ? Fully supported |
| Edge | 90+ | ? Fully supported |
| Safari | 14+ | - Partial (SSE issues) |

### Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-01 | Initial release |
| 1.1.0 | 2024-02 | Added sub-workflow support |
| 1.2.0 | 2024-03 | Loop & Branch nodes |
| 1.3.0 | 2024-04 | Database-driven node registry |

---

## Support & Contributing

### Getting Help
- GitHub Issues: https://github.com/techwayfit/Twf_AI_Framework/issues
- Documentation: This file
- Example Workflows: `/examples` directory (planned)

### Contributing Guidelines
1. Fork the repository
2. Create a feature branch
3. Follow coding standards (C# conventions, async/await)
4. Add tests for new features
5. Update documentation
6. Submit pull request

### Code Standards
- Use async/await for all I/O operations
- Follow repository pattern for data access
- Implement structured logging with scopes
- Add XML documentation comments
- Use nullable reference types
- Follow SOLID principles

---

**Last Updated:** 2024
**Framework Version:** .NET 10.0
**Document Version:** 1.0
