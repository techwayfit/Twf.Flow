# Twf.Flow - Web Application Quick Reference

**Quick lookup guide for common tasks and API endpoints**

---

## Table of Contents
- [Common Tasks](#common-tasks)
- [API Endpoints](#api-endpoints)
- [Code Snippets](#code-snippets)
- [Database Queries](#database-queries)
- [Troubleshooting](#troubleshooting)
- [Configuration](#configuration)

---

## Common Tasks

### Create a New Workflow

**UI:**
1. Navigate to `https://localhost:5001`
2. Click "Create New Workflow"
3. Enter name and description
4. Click "Create"

**API:**
```bash
curl -X POST https://localhost:5001/Workflow/Create \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Name=My Workflow&Description=Test workflow"
```

### Run a Workflow

**UI:**
1. Open workflow in designer
2. Click "Run" button
3. Enter initial data (JSON)
4. Click "Execute"

**API (Blocking):**
```bash
curl -X POST https://localhost:5001/Workflow/Run/{workflow-id} \
  -H "Content-Type: application/json" \
  -d '{"initialData": {"input": "Hello"}}'
```

**API (Streaming):**
```javascript
const eventSource = new EventSource('/Workflow/RunStream/{workflow-id}');

eventSource.addEventListener('node_complete', (e) => {
  console.log(JSON.parse(e.data));
});

eventSource.addEventListener('workflow_done', (e) => {
  console.log('Done:', JSON.parse(e.data));
  eventSource.close();
});
```

### Add a New Node Type

**UI:**
1. Navigate to `/Node`
2. Click "Create New Node Type"
3. Fill in form:
   - Node Type: `MyCustomNode`
   - Name: `My Custom Node`
   - Category: `Custom`
   - Schema JSON: (see schema example below)
4. Click "Create"

**Schema Example:**
```json
{
  "nodeType": "MyCustomNode",
  "displayName": "My Custom Node",
  "description": "Does something custom",
  "category": "Custom",
  "parameters": [
{
      "name": "apiKey",
    "displayName": "API Key",
      "type": "string",
      "required": true,
      "description": "API authentication key"
    }
  ],
  "inputs": [
    {
      "key": "data",
      "displayName": "Input Data",
      "required": true
    }
  ],
  "outputs": [
    {
 "key": "result",
      "displayName": "Result"
    }
  ]
}
```

### View Workflow Execution History

**UI:**
1. Navigate to `/Workflow/Runs/{workflow-id}`
2. Browse execution instances
3. Click instance ID to view details

**API:**
```bash
# Get all instances for a workflow
curl https://localhost:5001/Workflow/Runs/{workflow-id}

# Get specific instance details
curl https://localhost:5001/Workflow/RunDetail/{instance-id}
```

### Export/Import Workflows

**Export (manual):**
```bash
# Download workflow JSON
curl https://localhost:5001/Workflow/GetWorkflow/{id} > workflow.json
```

**Import (manual):**
```bash
# Upload workflow JSON
curl -X POST https://localhost:5001/Workflow/SaveWorkflow \
  -H "Content-Type: application/json" \
  -d @workflow.json
```

---

## API Endpoints

### Workflow Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/Workflow` | List all workflows | - |
| GET | `/Workflow/Details/{id}` | View workflow details | - |
| GET | `/Workflow/Create` | Create workflow form | - |
| POST | `/Workflow/Create` | Create new workflow | CSRF |
| GET | `/Workflow/Designer/{id}` | Open visual designer | - |
| GET | `/Workflow/Edit/{id}` | Edit workflow metadata | - |
| POST | `/Workflow/Edit/{id}` | Update workflow metadata | CSRF |
| DELETE | `/Workflow/Delete/{id}` | Delete workflow | CSRF |
| GET | `/Workflow/GetWorkflow/{id}` | Get workflow JSON | - |
| POST | `/Workflow/SaveWorkflow` | Save workflow JSON | - |
| GET | `/Workflow/GetAvailableNodes` | Get node types | - |
| GET | `/Workflow/GetNodeSchema/{type}` | Get node schema | - |
| GET | `/Workflow/GetAllNodeSchemas` | Get all schemas | - |

### Workflow Execution

| Method | Endpoint | Description | Returns |
|--------|----------|-------------|---------|
| GET | `/Workflow/Runner/{id}` | Workflow runner UI | HTML |
| POST | `/Workflow/Run/{id}` | Execute workflow | JSON |
| POST | `/Workflow/RunStream/{id}` | Execute (streaming) | SSE |
| GET | `/Workflow/Runs/{id}` | Execution history | HTML |
| GET | `/Workflow/RunDetail/{instanceId}` | Run details | HTML |

### Node Type Management

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/Node` | List node types | - |
| GET | `/Node?category={cat}` | Filter by category | - |
| GET | `/Node/Create` | Create node type form | - |
| POST | `/Node/Create` | Create node type | CSRF |
| GET | `/Node/Edit/{id}` | Edit node type | - |
| POST | `/Node/Edit/{id}` | Update node type | CSRF |
| DELETE | `/Node/Delete/{id}` | Delete node type | CSRF |
| POST | `/Node/ToggleEnabled/{id}` | Enable/disable node | CSRF |

---

## Code Snippets

### Create a Custom Node (C#)

```csharp
using Twf.Flow.Core;

namespace MyProject.Nodes;

public class MyCustomNode : INode
{
    public string Name => "My Custom Node";
    public string Description => "Does something custom";

    public List<DataPort> DataIn => new()
    {
        new DataPort("input_data", "Input Data", required: true)
    };

    public List<DataPort> DataOut => new()
    {
        new DataPort("output_data", "Output Data")
    };

    public async Task<WorkflowData> ExecuteAsync(
        WorkflowData data,
        WorkflowContext context)
    {
        var input = data.GetString("input_data");
     
        // Your custom logic here
   var result = await ProcessAsync(input, context.CancellationToken);
        
    return data.Clone().Set("output_data", result);
  }

  private async Task<string> ProcessAsync(string input, CancellationToken ct)
    {
        // Implementation
        await Task.Delay(100, ct);
   return input.ToUpper();
    }
}
```

### Register Custom Node in Database

```csharp
using Twf.Flow.Web.Data;
using Twf.Flow.Web.Repositories;

// In a seeder or startup service
public class CustomNodeSeeder
{
    public async Task SeedAsync(INodeTypeRepository repository)
    {
        var entity = new NodeTypeEntity
        {
 NodeType = "MyCustomNode",
 Name = "My Custom Node",
   Category = "Custom",
            Description = "Does something custom",
     Color = "#FF6B6B",
Icon = "bi-star",
   IdPrefix = "custom",
        FullTypeName = "MyProject.Nodes.MyCustomNode, MyProject",
   IsEnabled = true,
            SchemaJson = JsonSerializer.Serialize(new NodeParameterSchema
      {
                NodeType = "MyCustomNode",
     DisplayName = "My Custom Node",
      Description = "Does something custom",
      Category = "Custom",
        Parameters = new List<ParameterDefinition>
    {
  new()
    {
    Name = "setting",
                  DisplayName = "Setting",
        Type = "string",
    Required = false,
      DefaultValue = "default"
       }
    },
     Inputs = new List<PortSchema>
       {
 new() { Key = "input_data", DisplayName = "Input Data", Required = true }
    },
      Outputs = new List<PortSchema>
     {
         new() { Key = "output_data", DisplayName = "Output Data" }
     }
            })
        };

 await repository.CreateAsync(entity);
    }
}
```

### Execute Workflow Programmatically

```csharp
using Twf.Flow.Web.Services;
using Twf.Flow.Core;

public class MyService
{
    private readonly WorkflowDefinitionRunner _runner;
    private readonly IWorkflowRepository _repository;

    public async Task<WorkflowRunResult> RunWorkflowAsync(
   Guid workflowId,
    Dictionary<string, object> inputData)
    {
        // Load workflow
        var workflow = await _repository.GetByIdAsync(workflowId);
      if (workflow == null)
            throw new KeyNotFoundException($"Workflow {workflowId} not found");

        // Prepare initial data
        var data = new WorkflowData();
        foreach (var (key, value) in inputData)
            data.Set(key, value);

    // Execute
        var result = await _runner.RunAsync(workflow, data);

        return result;
    }
}
```

### Custom Variable Resolver

```csharp
using Twf.Flow.Web.Services.VariableResolution;
using Twf.Flow.Core;

public class CustomVariableResolver : IVariableResolver
{
    public object? Resolve(string template, WorkflowData data)
    {
 // Custom template syntax: ${variable}
      if (!template.StartsWith("${") || !template.EndsWith("}"))
            return template;

        var key = template[2..^1]; // Extract "variable"
        return data.Get<object>(key);
    }
}

// Register in Program.cs
builder.Services.AddSingleton<IVariableResolver, CustomVariableResolver>();
```

### Add Workflow Middleware

```csharp
public class WorkflowAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WorkflowAuditMiddleware> _logger;

    public WorkflowAuditMiddleware(
    RequestDelegate next,
        ILogger<WorkflowAuditMiddleware> logger)
    {
   _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/Workflow/Run"))
        {
          var startTime = DateTime.UtcNow;
         
          await _next(context);

  var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
   "Workflow execution: {Path} completed in {DurationMs}ms with status {StatusCode}",
    context.Request.Path,
     duration.TotalMilliseconds,
    context.Response.StatusCode);
        }
        else
    {
            await _next(context);
        }
    }
}

// Register in Program.cs
app.UseMiddleware<WorkflowAuditMiddleware>();
```

---

## Database Queries

### Common SQLite Queries

**List all workflows:**
```sql
SELECT Id, Name, Description, CreatedAt, UpdatedAt
FROM Workflows
ORDER BY UpdatedAt DESC;
```

**Search workflows by name:**
```sql
SELECT Id, Name, Description
FROM Workflows
WHERE Name LIKE '%search term%'
ORDER BY Name;
```

**Get workflow with full JSON:**
```sql
SELECT Id, Name, JsonData
FROM Workflows
WHERE Id = 'workflow-guid';
```

**List all enabled node types:**
```sql
SELECT NodeType, Name, Category, Description, Color, Icon
FROM NodeTypes
WHERE IsEnabled = 1
ORDER BY Category, Name;
```

**Get node type schema:**
```sql
SELECT SchemaJson
FROM NodeTypes
WHERE NodeType = 'LlmNode';
```

**Recent workflow executions:**
```sql
SELECT 
 wi.Id,
    wi.WorkflowName,
    wi.Status,
    wi.StartedAt,
    wi.CompletedAt,
 (julianday(wi.CompletedAt) - julianday(wi.StartedAt)) * 86400 AS DurationSeconds
FROM WorkflowInstances wi
ORDER BY wi.StartedAt DESC
LIMIT 20;
```

**Failed workflow runs:**
```sql
SELECT 
    wi.Id,
    wi.WorkflowName,
    wi.StartedAt,
    json_extract(wi.JsonData, '$.errorMessage') AS ErrorMessage,
    json_extract(wi.JsonData, '$.failedNodeName') AS FailedNode
FROM WorkflowInstances wi
WHERE wi.Status = 'Failed'
ORDER BY wi.StartedAt DESC;
```

**Workflow execution statistics:**
```sql
SELECT 
    w.Name,
    COUNT(wi.Id) AS TotalRuns,
 SUM(CASE WHEN wi.Status = 'Completed' THEN 1 ELSE 0 END) AS SuccessfulRuns,
    SUM(CASE WHEN wi.Status = 'Failed' THEN 1 ELSE 0 END) AS FailedRuns,
    AVG(julianday(wi.CompletedAt) - julianday(wi.StartedAt)) * 86400 AS AvgDurationSeconds
FROM Workflows w
LEFT JOIN WorkflowInstances wi ON w.Id = wi.WorkflowDefinitionId
GROUP BY w.Id, w.Name
ORDER BY TotalRuns DESC;
```

**Node usage frequency:**
```sql
-- Extract node types from workflow JSON (requires json_each)
SELECT 
    json_extract(value, '$.type') AS NodeType,
    COUNT(*) AS UsageCount
FROM Workflows,
    json_each(json_extract(Workflows.JsonData, '$.nodes'))
GROUP BY NodeType
ORDER BY UsageCount DESC;
```

### Backup and Restore

**Backup database:**
```bash
# Copy database file
cp workflows.db workflows_backup_$(date +%Y%m%d_%H%M%S).db

# Or use SQLite backup command
sqlite3 workflows.db ".backup workflows_backup.db"
```

**Restore database:**
```bash
# Stop application first
systemctl stop twf-ai-framework

# Restore backup
cp workflows_backup.db workflows.db

# Restart application
systemctl start twf-ai-framework
```

**Export workflows to JSON:**
```bash
sqlite3 workflows.db <<EOF
.mode json
.output workflows_export.json
SELECT Id, Name, Description, JsonData, CreatedAt, UpdatedAt FROM Workflows;
.quit
EOF
```

---

## Troubleshooting

### Enable Debug Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
  "Twf.Flow.Web": "Trace",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**View logs (console):**
```bash
dotnet run | grep -E "(ERROR|WARN|workflow)"
```

**View logs (structured):**
```bash
dotnet run 2>&1 | jq -r 'select(.Level == "Error" or .Level == "Warning")'
```

### Common Error Messages

#### "Workflow has no Start node"
**Cause:** Workflow definition missing StartNode  
**Fix:**
1. Open workflow in designer
2. Drag a "Start" node onto canvas
3. Connect to first actual node
4. Save workflow

#### "Node type 'XxxNode' not found"
**Cause:** Node type not registered or disabled  
**Fix:**
```sql
-- Check if node exists
SELECT * FROM NodeTypes WHERE NodeType = 'XxxNode';

-- Enable if disabled
UPDATE NodeTypes SET IsEnabled = 1 WHERE NodeType = 'XxxNode';
```

#### "Missing required input(s): xxx"
**Cause:** Node missing required input data  
**Fix:**
1. Check previous nodes output the required data
2. Or provide in initial data:
```json
{
  "initialData": {
    "xxx": "required_value"
  }
}
```

#### "Workflow exceeded maximum step limit"
**Cause:** Infinite loop in workflow  
**Fix:**
1. Check for circular connections
2. Add loop exit conditions
3. Use LoopNode with maxIterations

#### "Database is locked"
**Cause:** Multiple processes accessing SQLite  
**Fix:**
```bash
# Check for lock
lsof workflows.db

# Kill process holding lock
kill -9 <PID>

# Or restart application
systemctl restart twf-ai-framework
```

### Performance Issues

**Slow workflow execution:**
1. Check logs for slow nodes
2. Add timeout to slow nodes:
```json
{
  "executionOptions": {
    "timeoutMs": 30000
  }
}
```

**High memory usage:**
1. Check for large data in WorkflowData
2. Clear unnecessary data between nodes
3. Use scoped outputs to reduce memory

**Database growing too large:**
```bash
# Check database size
ls -lh workflows.db

# Clean old instances
sqlite3 workflows.db "DELETE FROM WorkflowInstances WHERE StartedAt < datetime('now', '-30 days');"

# Vacuum to reclaim space
sqlite3 workflows.db "VACUUM;"
```

### Reset Database

```bash
# Stop application
systemctl stop twf-ai-framework

# Delete database
rm workflows.db

# Start application (will recreate)
systemctl start twf-ai-framework
```

---

## Configuration

### Environment Variables

```bash
# Set connection string
export ConnectionStrings__WorkflowDb="Data Source=/var/data/workflows.db"

# Set log level
export Logging__LogLevel__Default="Debug"

# Set ASPNETCORE environment
export ASPNETCORE_ENVIRONMENT="Production"
```

### appsettings.json Quick Reference

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",        // Global log level
      "Microsoft.AspNetCore": "Warning",     // ASP.NET Core logs
      "Microsoft.EntityFrameworkCore": "Warning", // EF Core logs
      "Twf.Flow.Web": "Debug"         // App-specific logs
    }
  },
  "AllowedHosts": "*",  // CORS allowed hosts
  "UseDatabase": true,    // Use SQLite (vs JSON files)
  "WorkflowDataDirectory": "workflows",      // JSON file directory (if UseDatabase=false)
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=workflows.db" // SQLite connection string
  }
}
```

### Common Configuration Scenarios

**Development (verbose logging):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
   "Twf.Flow.Web": "Trace"
    }
  },
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=workflows-dev.db"
  }
}
```

**Production (minimal logging):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Twf.Flow.Web": "Information"
    }
  },
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=/var/data/workflows.db"
  }
}
```

**Testing (in-memory database):**
```json
{
  "ConnectionStrings": {
    "WorkflowDb": "Data Source=:memory:"
  }
}
```

---

## Keyboard Shortcuts (Designer)

| Key | Action |
|-----|--------|
| Delete | Delete selected node/connection |
| Escape | Deselect all |
| Ctrl+S | Save workflow |
| Ctrl+C | Copy selected node (planned) |
| Ctrl+V | Paste node (planned) |
| Ctrl+Z | Undo (planned) |
| Ctrl+Y | Redo (planned) |
| Ctrl++ | Zoom in |
| Ctrl+- | Zoom out |
| Ctrl+0 | Reset zoom |

---

## Useful Links

- **Application:** `https://localhost:5001`
- **Workflows:** `https://localhost:5001/Workflow`
- **Designer:** `https://localhost:5001/Workflow/Designer/{id}`
- **Node Types:** `https://localhost:5001/Node`
- **Swagger (if enabled):** `https://localhost:5001/swagger`

---

## Quick Tips

### Designer
- **Double-click** node to edit properties
- **Shift+drag** to pan canvas
- **Scroll** to zoom
- **Drag from port** to create connection
- **Click connection** to select for deletion

### Workflow Data Access
```javascript
// In node parameters, use template syntax:
"prompt": "Hello {{user_name}}"      // Simple variable
"url": "{{api_url}}/{{endpoint}}"// Multiple variables
"text": "{{llm001.response}}"         // Node output reference
"data": "{{items[0].name}}"    // Array access (planned)
```

### Performance
- Use **streaming execution** for real-time UI updates
- Enable **caching** on slow nodes
- Add **timeouts** to prevent hanging
- Use **sub-workflows** to organize complex flows
- **Disable unused node types** to reduce palette clutter

### Debugging
- Check browser console for JS errors
- Check server logs for execution errors
- Use **RunDetail** view to see step-by-step execution
- Enable **Debug** logging for detailed traces
- Use **Correlation IDs** to track requests

---

**Last Updated:** 2024
**Framework Version:** .NET 10.0  
**Document Version:** 1.0
