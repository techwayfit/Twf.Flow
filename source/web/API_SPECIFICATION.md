# Twf.Flow - Web API Specification

**RESTful API specification for the Twf.Flow Web Application**

Version: 1.0  
Base URL: `https://localhost:5001`  
Protocol: HTTPS
Authentication: None (planned for future)

---

## Table of Contents
- [API Overview](#api-overview)
- [Workflow Management API](#workflow-management-api)
- [Workflow Execution API](#workflow-execution-api)
- [Node Type Management API](#node-type-management-api)
- [Data Models](#data-models)
- [Error Responses](#error-responses)
- [Rate Limiting](#rate-limiting)
- [Webhooks](#webhooks)

---

## API Overview

### Content Types

**Request:**
- `application/json` - JSON payloads
- `application/x-www-form-urlencoded` - Form submissions
- `multipart/form-data` - File uploads (planned)

**Response:**
- `application/json` - JSON responses
- `text/html` - HTML views
- `text/event-stream` - Server-Sent Events (SSE)
- `application/problem+json` - Error responses

### HTTP Status Codes

| Code | Description | Usage |
|------|-------------|-------|
| 200 | OK | Successful GET/PUT/DELETE |
| 201 | Created | Successful POST (resource created) |
| 204 | No Content | Successful DELETE (no body) |
| 400 | Bad Request | Invalid request payload |
| 404 | Not Found | Resource not found |
| 422 | Unprocessable Entity | Business logic error |
| 500 | Internal Server Error | Unexpected server error |
| 504 | Gateway Timeout | Request timeout |

### Headers

**Request Headers:**
```http
Content-Type: application/json
Accept: application/json
X-Correlation-ID: abc-123-def (optional)
```

**Response Headers:**
```http
Content-Type: application/json
X-Correlation-ID: abc-123-def
```

---

## Workflow Management API

### List Workflows

**Endpoint:** `GET /Workflow`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search workflows by name |

**Response:** HTML view with workflow list

**Example:**
```http
GET /Workflow?search=sentiment
```

---

### Get Workflow JSON

**Endpoint:** `GET /Workflow/GetWorkflow/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | Yes | Workflow ID |

**Response:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Sentiment Analysis",
  "description": "Analyzes sentiment of text",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-02T12:30:00Z",
  "nodes": [
    {
      "id": "456e7890-e89b-12d3-a456-426614174001",
      "nodeId": "start001",
    "name": "Start",
      "type": "StartNode",
      "category": "Control",
      "parameters": {},
      "position": { "x": 100, "y": 100 },
      "color": "#4CAF50"
    },
    {
      "id": "789e0123-e89b-12d3-a456-426614174002",
      "nodeId": "llm001",
   "name": "Sentiment LLM",
      "type": "LlmNode",
      "category": "AI",
      "parameters": {
      "provider": "openai",
      "model": "gpt-4",
"temperature": 0.7
    },
      "position": { "x": 300, "y": 100 },
      "color": "#2196F3",
      "executionOptions": {
     "maxRetries": 3,
        "retryDelayMs": 1000,
        "timeoutMs": 30000
      }
    }
  ],
  "connections": [
    {
      "id": "abc-123-def",
      "sourceNodeId": "456e7890-e89b-12d3-a456-426614174001",
   "sourcePort": "output",
      "targetNodeId": "789e0123-e89b-12d3-a456-426614174002",
      "targetPort": "input",
 "label": null
    }
  ],
  "variables": {
    "api_url": "https://api.example.com",
    "max_retries": 3
  },
  "errorNodeId": null,
  "subWorkflows": [],
  "metadata": {
    "author": "John Doe",
  "tags": ["sentiment", "nlp"],
    "version": 1,
    "isActive": true
  }
}
```

**Status Codes:**
- `200 OK` - Workflow found
- `404 Not Found` - Workflow does not exist

---

### Save Workflow

**Endpoint:** `POST /Workflow/SaveWorkflow`

**Request Body:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Updated Workflow",
  "description": "Updated description",
  "nodes": [...],
  "connections": [...],
  "variables": {},
  "metadata": {}
}
```

**Response:**
```json
{
  "success": true,
  "id": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Status Codes:**
- `200 OK` - Workflow saved successfully
- `400 Bad Request` - Invalid payload
- `500 Internal Server Error` - Save failed

**Example:**
```bash
curl -X POST https://localhost:5001/Workflow/SaveWorkflow \
  -H "Content-Type: application/json" \
  -d @workflow.json
```

---

### Delete Workflow

**Endpoint:** `POST /Workflow/Delete/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | Yes | Workflow ID |

**Request Headers:**
```http
Content-Type: application/x-www-form-urlencoded
RequestVerificationToken: <anti-forgery-token>
```

**Response:** Redirect to `/Workflow` (302)

**Status Codes:**
- `302 Found` - Redirect after deletion
- `404 Not Found` - Workflow does not exist

---

### Get Available Nodes

**Endpoint:** `GET /Workflow/GetAvailableNodes`

**Response:**
```json
[
  {
    "type": "LlmNode",
    "category": "AI",
  "name": "LLM",
    "description": "Call a Large Language Model",
    "color": "#2196F3",
    "icon": "bi-cpu",
    "idPrefix": "llm",
    "fullTypeName": "Twf.Flow.Nodes.AI.LlmNode, Twf.Flow.Core",
    "defaultParams": {
    "provider": "openai",
      "model": "gpt-4",
      "temperature": 0.7
    }
  },
  {
    "type": "HttpRequestNode",
    "category": "IO",
    "name": "HTTP Request",
    "description": "Make an HTTP request",
    "color": "#FF9800",
    "icon": "bi-globe",
    "idPrefix": "http",
    "fullTypeName": "Twf.Flow.Nodes.IO.HttpRequestNode, Twf.Flow.Core",
    "defaultParams": {
"method": "GET",
    "url": "https://api.example.com"
    }
  }
]
```

**Status Codes:**
- `200 OK` - Nodes retrieved

---

### Get Node Schema

**Endpoint:** `GET /Workflow/GetNodeSchema/{nodeType}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| nodeType | string | Yes | Node type name (e.g., "LlmNode") |

**Response:**
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
    "description": "LLM provider (openai, anthropic, azure)",
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
      "description": "Sampling temperature (0.0 to 2.0)",
      "min": 0.0,
      "max": 2.0
    }
  ],
  "inputs": [
    {
      "key": "prompt",
 "displayName": "Prompt",
      "required": true,
      "description": "The prompt text to send to the LLM"
    },
    {
   "key": "system_message",
      "displayName": "System Message",
   "required": false,
      "description": "Optional system message"
    }
  ],
  "outputs": [
    {
      "key": "response",
      "displayName": "Response",
      "description": "The LLM's text response"
    },
    {
      "key": "usage",
      "displayName": "Token Usage",
      "description": "Token usage statistics"
    }
  ]
}
```

**Status Codes:**
- `200 OK` - Schema found
- `404 Not Found` - Node type does not exist

---

### Get All Node Schemas

**Endpoint:** `GET /Workflow/GetAllNodeSchemas`

**Response:**
```json
{
  "LlmNode": { ... },
  "HttpRequestNode": { ... },
  "BranchNode": { ... }
}
```

**Status Codes:**
- `200 OK` - Schemas retrieved

---

## Workflow Execution API

### Execute Workflow (Blocking)

**Endpoint:** `POST /Workflow/Run/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | Yes | Workflow ID |

**Request Body:**
```json
{
  "initialData": {
    "input_text": "Analyze this text for sentiment",
  "user_id": "user_123",
    "api_key": "sk-..."
  }
}
```

**Response (Success):**
```json
{
  "result": {
  "isSuccess": true,
    "workflowName": "Sentiment Analysis",
    "outputData": {
      "sentiment": "positive",
    "confidence": 0.95,
      "llm001.response": "The sentiment is positive with high confidence.",
      "llm001.usage": {
      "prompt_tokens": 50,
        "completion_tokens": 20,
        "total_tokens": 70
    }
    },
    "errorMessage": null,
    "failedNodeName": null
  },
  "instanceId": "789e0123-e89b-12d3-a456-426614174003"
}
```

**Response (Failure):**
```json
{
  "result": {
    "isSuccess": false,
    "workflowName": "Sentiment Analysis",
    "outputData": {
      "input_text": "Analyze this text for sentiment"
    },
    "errorMessage": "API request failed: 401 Unauthorized",
    "failedNodeName": "Sentiment LLM"
  },
  "instanceId": "789e0123-e89b-12d3-a456-426614174003"
}
```

**Status Codes:**
- `200 OK` - Workflow executed successfully
- `422 Unprocessable Entity` - Workflow execution failed
- `404 Not Found` - Workflow does not exist
- `500 Internal Server Error` - Unexpected error

**Example:**
```bash
curl -X POST https://localhost:5001/Workflow/Run/123e4567-... \
  -H "Content-Type: application/json" \
  -d '{"initialData": {"input_text": "Hello world"}}'
```

---

### Execute Workflow (Streaming)

**Endpoint:** `POST /Workflow/RunStream/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | Yes | Workflow ID |

**Request Body:**
```json
{
  "initialData": {
    "input_text": "Analyze this text"
  }
}
```

**Response:** Server-Sent Events (SSE) stream

**Content-Type:** `text/event-stream`

**Events:**

1. **node_start**
```
event: node_start
data: {"eventType":"node_start","nodeId":"456e7890-...","nodeName":"Sentiment LLM","nodeType":"LlmNode","timestamp":"2024-01-01T12:00:00Z","inputs":{"prompt":"Analyze: Hello world"}}
```

2. **node_complete**
```
event: node_complete
data: {"eventType":"node_complete","nodeId":"456e7890-...","nodeName":"Sentiment LLM","nodeType":"LlmNode","timestamp":"2024-01-01T12:00:02Z","outputs":{"response":"Positive sentiment","usage":{"total_tokens":70}}}
```

3. **node_error** (if node fails but workflow continues)
```
event: node_error
data: {"eventType":"node_error","nodeId":"456e7890-...","nodeName":"Sentiment LLM","nodeType":"LlmNode","timestamp":"2024-01-01T12:00:02Z","errorMessage":"API timeout"}
```

4. **loop_iteration_start** (for LoopNode)
```
event: loop_iteration_start
data: {"eventType":"loop_iteration_start","nodeId":"789e0123-...","nodeName":"Process Items","nodeType":"LoopNode"}
```

5. **workflow_done** (success)
```
event: workflow_done
data: {"result":{"isSuccess":true,"workflowName":"Sentiment Analysis","outputData":{...}},"instanceId":"abc-123-def"}
```

6. **workflow_error** (failure)
```
event: workflow_error
data: {"result":{"isSuccess":false,"workflowName":"Sentiment Analysis","outputData":{...},"errorMessage":"Node failed","failedNodeName":"Sentiment LLM"},"instanceId":"abc-123-def"}
```

**Client Example (JavaScript):**
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

eventSource.addEventListener('workflow_error', (e) => {
  const data = JSON.parse(e.data);
  console.error('Workflow failed!', data.result);
  eventSource.close();
});

eventSource.onerror = (error) => {
  console.error('SSE error:', error);
  eventSource.close();
};
```

**Status Codes:**
- `200 OK` - Stream started
- `404 Not Found` - Workflow does not exist

---

### View Workflow Runs

**Endpoint:** `GET /Workflow/Runs/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | Yes | Workflow ID |

**Response:** HTML view with execution history

---

### View Run Detail

**Endpoint:** `GET /Workflow/RunDetail/{instanceId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| instanceId | Guid | Yes | Workflow instance ID |

**Response:** HTML view with step-by-step execution log

---

## Node Type Management API

### List Node Types

**Endpoint:** `GET /Node`

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| category | string | No | Filter by category |

**Response:** HTML view with node type list

**Example:**
```http
GET /Node?category=AI
```

---

### Create Node Type

**Endpoint:** `POST /Node/Create`

**Request Body (Form):**
```
NodeType=MyCustomNode
Name=My Custom Node
Category=Custom
Description=Does something custom
Color=#FF6B6B
Icon=bi-star
IdPrefix=custom
FullTypeName=MyProject.Nodes.MyCustomNode, MyProject
IsEnabled=true
SchemaJson={"nodeType":"MyCustomNode",...}
```

**Response:** Redirect to `/Node` (302)

**Status Codes:**
- `302 Found` - Redirect after creation
- `400 Bad Request` - Invalid schema JSON or duplicate node type

---

### Update Node Type

**Endpoint:** `POST /Node/Edit/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | int | Yes | Node type ID |

**Request Body:** Same as Create

**Response:** Redirect to `/Node` (302)

---

### Delete Node Type

**Endpoint:** `POST /Node/Delete/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | int | Yes | Node type ID |

**Response:** Redirect to `/Node` (302)

---

### Toggle Node Type Enabled

**Endpoint:** `POST /Node/ToggleEnabled/{id}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | int | Yes | Node type ID |

**Response:** Redirect to `/Node` (302)

---

## Data Models

### WorkflowDefinition

```typescript
interface WorkflowDefinition {
  id: string;    // Guid
  name: string;
  description?: string;
  createdAt: string;           // ISO 8601 date
  updatedAt: string;
  nodes: NodeDefinition[];
  connections: ConnectionDefinition[];
  variables: Record<string, any>;
  errorNodeId?: string;  // Guid
  subWorkflows: ChildWorkflowDefinition[];
  metadata: WorkflowMetadata;
}
```

### NodeDefinition

```typescript
interface NodeDefinition {
  id: string;          // Guid
  nodeId: string;     // Human-readable ID (e.g., "llm001")
  name: string;
  type: string;  // Node type (e.g., "LlmNode")
  category: string;    // Category (e.g., "AI")
  parameters: Record<string, any>;
  position: { x: number; y: number };
  color?: string;
  executionOptions?: NodeExecutionOptions;
  subWorkflow?: SubWorkflowDefinition;
  isExpanded: boolean;
}
```

### NodeExecutionOptions

```typescript
interface NodeExecutionOptions {
  maxRetries: number;          // Default: 0
  retryDelayMs: number;        // Default: 1000
  timeoutMs?: number;
  continueOnError: boolean;    // Default: false
  runCondition?: string;       // e.g., "{{should_run}} == true"
  fallbackData?: Record<string, any>;
}
```

### ConnectionDefinition

```typescript
interface ConnectionDefinition {
  id: string;              // Guid
  sourceNodeId: string;        // Guid
sourcePort: string;          // e.g., "output", "success", "error"
  targetNodeId: string;        // Guid
  targetPort: string;      // e.g., "input"
  label?: string;
}
```

### WorkflowRunResult

```typescript
interface WorkflowRunResult {
  isSuccess: boolean;
  workflowName: string;
  outputData: Record<string, any>;
  errorMessage?: string;
  failedNodeName?: string;
}
```

### NodeStepEvent

```typescript
interface NodeStepEvent {
  eventType: "node_start" | "node_complete" | "node_error" | "loop_iteration_start";
  nodeId: string;       // Guid
  nodeName: string;
  nodeType: string;
  timestamp: string;    // ISO 8601
  inputs?: Record<string, any>;
  outputs?: Record<string, any>;
  errorMessage?: string;
}
```

### NodeParameterSchema

```typescript
interface NodeParameterSchema {
  nodeType: string;
  displayName: string;
  description: string;
  category: string;
  parameters: ParameterDefinition[];
  inputs: PortSchema[];
outputs: PortSchema[];
}

interface ParameterDefinition {
  name: string;
  displayName: string;
  type: "string" | "number" | "boolean" | "object" | "array";
  required: boolean;
  defaultValue?: any;
  description?: string;
  options?: string[];          // For enum/select
  min?: number;     // For number
  max?: number;
}

interface PortSchema {
  key: string;
  displayName: string;
  required?: boolean;
  description?: string;
}
```

---

## Error Responses

All errors follow [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807) format.

### Standard Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid workflow definition: missing Start node",
  "instance": "/Workflow/SaveWorkflow",
  "correlationId": "abc-123-def",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### Error Types

**400 Bad Request:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid JSON payload"
}
```

**404 Not Found:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Workflow '123e4567-...' not found"
}
```

**422 Unprocessable Entity:**
```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "Workflow execution failed: Node 'LLM' missing required input 'prompt'"
}
```

**500 Internal Server Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later.",
  "correlationId": "abc-123-def",
  "exceptionType": "NullReferenceException"
}
```

**504 Gateway Timeout:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.5",
  "title": "Gateway Timeout",
  "status": 504,
  "detail": "Operation timed out after 30 seconds"
}
```

---

## Rate Limiting

**Status:** Not implemented (planned for future)

**Planned Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

**Rate Limit Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 60 seconds.",
  "retryAfter": 60
}
```

---

## Webhooks

**Status:** Not implemented (planned for future)

**Planned Events:**
- `workflow.execution.started`
- `workflow.execution.completed`
- `workflow.execution.failed`
- `workflow.created`
- `workflow.updated`
- `workflow.deleted`

**Webhook Payload Example:**
```json
{
  "event": "workflow.execution.completed",
  "timestamp": "2024-01-01T12:00:00Z",
  "data": {
    "workflowId": "123e4567-...",
    "workflowName": "Sentiment Analysis",
    "instanceId": "789e0123-...",
    "status": "Completed",
    "duration": 2.5,
    "outputData": { ... }
  }
}
```

---

## Versioning

**Current Version:** 1.0  
**Versioning Strategy:** Not implemented (all endpoints are v1 by default)

**Planned:**
- URL versioning: `/api/v2/Workflow/...`
- Header versioning: `Accept: application/vnd.twf.v2+json`

---

## CORS

**Status:** Configured in `Program.cs`

**Allowed Origins:** `*` (configurable via `AllowedHosts`)  
**Allowed Methods:** `GET, POST, PUT, DELETE`  
**Allowed Headers:** `Content-Type, Authorization, X-Correlation-ID`

---

## Security

### HTTPS

All endpoints **must** use HTTPS in production.

### CSRF Protection

All state-changing endpoints (`POST`, `DELETE`) require anti-forgery tokens:

```html
<form method="post" action="/Workflow/Delete/123">
  @Html.AntiForgeryToken()
  <button type="submit">Delete</button>
</form>
```

### Authentication

**Status:** Not implemented (planned for future)

**Planned:**
- Bearer token authentication
- API key authentication
- JWT tokens

---

## Change Log

### Version 1.0 (2024-01)
- Initial API release
- Workflow CRUD operations
- Workflow execution (blocking & streaming)
- Node type management
- SSE support for real-time execution

---

**Last Updated:** 2024  
**Framework Version:** .NET 10.0  
**API Version:** 1.0
