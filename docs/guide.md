# Twf.Flow — Workflow Designer Guide

This guide covers everything you need to build, configure, and run workflows in the Twf.Flow designer, including all available nodes, sub-workflow patterns, data flow, and error handling.

---

## Table of Contents

1. [Opening the Designer](#1-opening-the-designer)
2. [Designer Layout](#2-designer-layout)
3. [Building Your First Workflow](#3-building-your-first-workflow)
4. [Node Reference — Control](#4-node-reference--control)
5. [Node Reference — AI](#5-node-reference--ai)
6. [Node Reference — Data](#6-node-reference--data)
7. [Node Reference — IO](#7-node-reference--io)
8. [Data Flow: WorkflowData](#8-data-flow-workflowdata)
9. [Sub-Workflows](#9-sub-workflows)
10. [Error Handling](#10-error-handling)
11. [Data Input Mapping](#11-data-input-mapping)
12. [Node Execution Options](#12-node-execution-options)
13. [Variables](#13-variables)
14. [Common Workflow Patterns](#14-common-workflow-patterns)

---

## 1. Opening the Designer

| URL pattern | What opens |
|---|---|
| `/Workflow` | Workflow list — create, search, open, delete workflows |
| `/Workflow/Designer/{id}` | Open the main workflow canvas |
| `/Workflow/Designer/{id}?subWorkflowId={subId}` | Open a specific sub-workflow directly |
| `/{mainWorkflowId}/{subWorkflowId}` | Deep link to a sub-workflow |

When you navigate to a sub-workflow URL and refresh the page, the designer automatically restores the sub-workflow editing context. Use the **Main Flow** button in the top toolbar to return to the root workflow canvas.

---

## 2. Designer Layout

The designer is divided into four areas:

```
┌─────────────────────────────────────────────────────┐
│  Top toolbar: Zoom · Save · Delete · Main Flow      │
├────────────┬────────────────────────────┬────────────┤
│            │                            │            │
│  Left      │       Center Canvas        │  Right     │
│  Sidebar   │                            │  Panel     │
│            │  Nodes · Connections       │            │
│  Nodes tab │  Port handles              │  Node      │
│  Vars tab  │  Drag to move             │  Properties│
│            │                            │            │
└────────────┴────────────────────────────┴────────────┘
```

### Left Sidebar — Nodes Tab

Contains all available nodes grouped by category. Drag any node from the sidebar onto the canvas to add it.

Categories:
- **Control** — Start, End, Error Handler, Condition, Branch, Sub Workflow, Loop, Parallel, Delay, Merge, Log
- **AI** — LLM, Prompt Builder, Embedding, Output Parser
- **Data** — Transform, Data Mapper, Filter, Chunk Text, Memory
- **IO** — HTTP Request

### Left Sidebar — Variables Tab

Manage workflow-level variables and sub-workflows. Variables defined here can be referenced in node parameters using `{{variable_name}}` syntax.

The **Sub Workflows** section shows all child workflows attached to this root workflow. Actions available:
- **Add sub-workflow** — create a new empty child workflow
- **Open** — switch the canvas to that sub-workflow
- **Open in new tab** — edit in a separate browser tab
- **Rename** — change the name
- **Delete** — only allowed if no `Sub Workflow` node currently references it

### Center Canvas

- **Drag** nodes from the sidebar to place them
- **Click** a node to select it and open its properties in the right panel
- **Drag** from an output port (right side of a node) to an input port (left side) to create a connection
- **Delete** a selected node or connection with the Delete toolbar button
- **Pan** by clicking and dragging empty canvas space
- **Zoom** with the toolbar zoom controls or mouse wheel

Port conventions:
- **Left side** of a node = input port(s)
- **Right side** of a node = output port(s)
- Output ports are labelled (`output`, `success`, `error`, `case1`, `case2`, `case3`, `default`, `true`, `false`)

### Right Panel — Node Properties

When a node is selected, configure its parameters here. The panel always shows:
- **Name** — editable display name for this node instance
- **Type** — read-only node type
- **Parameters** — type-specific fields (text, number, select, JSON, etc.)
- **Data Input** (for nodes with inputs) — JSON input mapping section
- **Execution Options** — retry, timeout, continue-on-error settings

---

## 3. Building Your First Workflow

Every valid workflow follows this baseline structure:

```
[Start] ──► [processing nodes] ──► [End]
```

Steps:

1. **Drag a Start node** onto the canvas. Every workflow must have exactly one Start node.
2. **Drag your processing nodes** — AI, Data, IO, or Control — and arrange them left to right.
3. **Drag an End node** onto the canvas.
4. **Connect the nodes** by dragging from the `output` port of one node to the `input` port of the next.
5. **Configure each node** by clicking it and filling in the properties on the right panel.
6. **Save** using the Save button in the top toolbar.

Recommended connection order:

```
Start ──► PromptBuilder ──► LLM ──► OutputParser ──► End
```

---

## 4. Node Reference — Control

Control nodes manage the flow of execution: where it starts, how it branches, how errors are handled, and how it finishes.

---

### Start

**Category:** Control  
**Color:** Green  
**Ports:** output (right)

The mandatory entry point of every workflow. Execution always begins here. There must be exactly one Start node per workflow (or sub-workflow).

No parameters to configure.

**Connection rule:** Connect the `output` port to the first processing node.

---

### End

**Category:** Control  
**Color:** Red  
**Ports:** input (left)

Marks the successful completion of a workflow path. When execution reaches an End node, the workflow is considered to have succeeded.

No parameters to configure.

**Connection rule:** Connect the last node's output to the End node's `input` port.

---

### Error Handler

**Category:** Control  
**Color:** Red  
**Ports:** output (right)

The workflow-level error entry point. **Maximum one per workflow** (including sub-workflows). It has **no input port** — it is not connected from any node. Instead, the runtime automatically routes execution here whenever an unhandled exception occurs anywhere in the workflow.

Connect the `output` port of the Error Handler to nodes that perform error recovery, logging, or notification.

**How it works:**
- When any node throws an unhandled exception, the runtime jumps to the Error Handler node in the same workflow.
- The WorkflowData at the point of failure is passed into the error branch.
- Each sub-workflow can have its own Error Handler, independently of the parent workflow's Error Handler.

---

### Condition

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), output (right)

Evaluates one or more boolean conditions against the current WorkflowData and writes the results as named boolean flags. Use this node to prepare routing decisions that a Branch or downstream node will act on.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Conditions | key-value JSON | Map of output flag name to condition expression |

**Outputs written to WorkflowData:**

Each condition key becomes a `true`/`false` value in the data bag. For example, configuring `is_positive` to evaluate `sentiment == "positive"` writes `is_positive: true` or `is_positive: false`.

**Common factory shortcuts (code):**
- `ConditionNode.HasKey(outputKey, checkKey)` — checks whether a key exists
- `ConditionNode.StringEquals(outputKey, dataKey, expectedValue)` — string comparison
- `ConditionNode.LengthExceeds(outputKey, dataKey, maxLength)` — length check

---

### Branch (Switch)

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), case1 / case2 / case3 / default (right)

Routes execution to one of up to four output ports based on value matching. Equivalent to a switch/case statement.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Value Key | Text | The WorkflowData key whose value is compared |
| Case 1 Value | Text | Value that routes to the `case1` port |
| Case 2 Value | Text | Value that routes to the `case2` port |
| Case 3 Value | Text | Value that routes to the `case3` port |
| Case Sensitive | Boolean | Whether matching is case-sensitive (default: false) |

**Outputs written to WorkflowData:**

| Key | Value |
|---|---|
| `branch_selected_port` | `"case1"` \| `"case2"` \| `"case3"` \| `"default"` |
| `branch_input_value` | The raw input value that was tested |
| `branch_selected_value` | The matched case value |
| `branch_case1` | `true` if case1 matched |
| `branch_case2` | `true` if case2 matched |
| `branch_case3` | `true` if case3 matched |
| `branch_default` | `true` if no case matched |

**Connection rule:** Connect the appropriate `case1`, `case2`, `case3`, or `default` output port to the first node of each route.

---

### Sub Workflow

**Category:** Control  
**Color:** Purple  
**Ports:** input (left), success / error (right)

Invokes a named child workflow as a single step. The child workflow receives a copy of the current WorkflowData, executes fully, and returns its result data back to the parent.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Target Sub-Workflow | Select | Pick from the sub-workflows listed in the Variables tab |

**Port behavior:**
- `success` — taken when the child workflow completes without errors
- `error` — taken when the child workflow fails (unhandled exception)

**Connection rule:**
```
[Previous Node] ──► [Sub Workflow] ──► success ──► [Next success path]
                                   └──► error  ──► [Error handler path]
```

See [Section 9 — Sub-Workflows](#9-sub-workflows) for full guidance on creating and managing sub-workflows.

---

### Loop (ForEach)

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), output (right)

Iterates over a list in WorkflowData and runs an embedded sub-workflow for every item. Results from all iterations are collected into a list.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Items Key | Text | WorkflowData key containing the list to iterate |
| Output Key | Text | WorkflowData key where the collected results are written |

**How it works:**
- Each list item is passed into the loop body workflow as an individual WorkflowData entry.
- The loop body runs once per item.
- All results are collected and placed in the `Output Key` list.

**Example:**
```
Items Key:  chunks        (a List<string> of text chunks)
Output Key: embeddings    (a List<float[]> of embedding results)
```

---

### Parallel

**Category:** Control  
**Color:** Purple  
**Ports:** input (left), output (right)

Runs multiple nodes simultaneously. Each parallel branch receives a clone of the current WorkflowData. When all branches complete, their results are merged back (later-completing branches overwrite earlier ones if keys conflict).

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Branches | Node list | The nodes to run in parallel |

**Use case:** Running multiple AI calls simultaneously, fetching from multiple APIs at the same time, or independent transformations that do not depend on one another.

---

### Delay

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), output (right)

Pauses execution for a specified duration before passing data to the next node. Useful for rate limiting, waiting for background processes, or simulating latency during testing.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Duration (ms) | Number | Delay duration in milliseconds |
| Reason | Text | Optional label for logging |

---

### Merge

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), output (right)

Combines multiple WorkflowData keys into a single string value. Useful for merging outputs from parallel branches or aggregating multiple partial results into one field.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Source Keys | String List | The WorkflowData keys to merge |
| Output Key | Text | The key where the merged result is written |
| Separator | Text | String placed between merged values (default: newline) |

---

### Log

**Category:** Control  
**Color:** Orange  
**Ports:** input (left), output (right)

A debugging checkpoint that logs the current WorkflowData state without modifying it. The node passes data through unchanged.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Label | Text | Checkpoint label shown in logs |
| Keys to Log | String List | Specific keys to log (empty = log all) |
| Log Level | Select | Information / Warning / Debug |

---

## 5. Node Reference — AI

AI nodes interact with large language model APIs and process their output.

---

### LLM (Large Language Model)

**Category:** AI  
**Color:** Blue  
**Ports:** input (left), output (right)

Calls any OpenAI-compatible LLM API — OpenAI, Anthropic Claude, Azure OpenAI, Ollama, and others.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Provider | Select | `openai` / `anthropic` / `azure` / `ollama` |
| Model | Text | Model name, e.g., `gpt-4o`, `claude-3-5-sonnet`, `llama3` |
| API Key | Text | API key (use `{{variable_name}}` to reference a workflow variable) |
| Base URL | Text | Endpoint URL (required for Azure OpenAI and Ollama) |
| System Prompt | TextArea | Default system instruction (can be overridden by upstream data) |
| Temperature | Number | 0.0–2.0, controls randomness |
| Max Tokens | Number | Maximum tokens in the response |
| Maintain History | Boolean | Whether to accumulate conversation history in context memory |

**Reads from WorkflowData:**

| Key | Description |
|---|---|
| `prompt` | The user message to send |
| `messages` | Full message array (overrides `prompt` when present) |
| `system_prompt` | System instruction (overrides the static parameter) |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `llm_response` | The model's text response |
| `llm_model` | Model name used |
| `prompt_tokens` | Token count for the prompt |
| `completion_tokens` | Token count for the completion |

---

### Prompt Builder

**Category:** AI  
**Color:** Blue  
**Ports:** input (left), output (right)

Builds dynamic prompts from templates using `{{variable}}` substitution. Variables are resolved from WorkflowData first, then from static node parameters.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Prompt Template | TextArea | The prompt template with `{{variable}}` placeholders |
| System Template | TextArea | Optional system prompt template |
| Static Variables | Key-Value List | Key-value pairs that override WorkflowData values |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `prompt` | The rendered prompt string |
| `system_prompt` | The rendered system prompt (if System Template is set) |

**Template example:**
```
You are a {{role}}. The customer said: "{{user_message}}". 
Respond in {{language}}.
```

If `role`, `user_message`, and `language` are present in WorkflowData (e.g., from a previous node), they are substituted. Unresolved variables are replaced with `{{MISSING:variable_name}}`.

---

### Embedding

**Category:** AI  
**Color:** Blue  
**Ports:** input (left), output (right)

Generates vector embeddings for text using an OpenAI-compatible embeddings API. Core building block for RAG pipelines and semantic search.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Model | Text | Embedding model name, e.g., `text-embedding-3-small` |
| API Key | Text | API key |
| Base URL | Text | Endpoint URL |

**Reads from WorkflowData:**

| Key | Description |
|---|---|
| `text` | Single string to embed |
| `texts` | `List<string>` for batch embedding (processed sequentially) |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `embedding` | `float[]` — single embedding vector |
| `embeddings` | `List<float[]>` — batch embedding vectors |
| `embedding_model` | Model name used |

---

### Output Parser

**Category:** AI  
**Color:** Blue  
**Ports:** input (left), output (right)

Parses structured JSON from LLM responses. Handles markdown code fences (` ```json `) automatically, extracts JSON objects or arrays, and maps selected fields into WorkflowData keys.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Field Mapping | Key-Value List | Map JSON keys to WorkflowData keys (empty = write all fields) |
| Strict Mode | Boolean | If true, throw an error when JSON cannot be parsed; if false, pass data through unchanged |

**Reads from WorkflowData:**

| Key | Description |
|---|---|
| `llm_response` | Raw LLM text output |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `parsed_output` | The full parsed JSON as a dictionary |
| *(mapped keys)* | Individual fields mapped per the Field Mapping configuration |

**Example field mapping:**

If the LLM returns:
```json
{"sentiment": "positive", "score": 0.92, "summary": "Customer is happy"}
```

And Field Mapping is:
```
sentiment  →  sentiment_result
score      →  confidence
```

Then WorkflowData will have `sentiment_result: "positive"` and `confidence: 0.92`.

---

## 6. Node Reference — Data

Data nodes transform, filter, map, chunk, and store the data that flows through the workflow.

---

### Transform

**Category:** Data  
**Color:** Green  
**Ports:** input (left), output (right)

Applies a custom transformation to WorkflowData. In the UI, this is configured with a transformation expression or a preset.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Transform Expression | TextArea | Custom logic expression or preset name |

**Built-in presets:**
- **Rename** — rename a key (`from_key` → `to_key`)
- **SelectKey** — copy one key's value to a different key name
- **ConcatStrings** — join multiple string keys into one, with a separator

---

### Data Mapper

**Category:** Data  
**Color:** Green  
**Ports:** input (left), output (right)

Explicitly maps values from source paths to target keys. More precise than Transform — use this when you need to wire a specific upstream output field into a different key name for the next node.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Mappings | Key-Value List | `target_key: source.path` pairs |
| Default Values | Key-Value List | Fallback values for missing mappings |
| Throw on Missing | Boolean | Error if a source path is not found |
| Remove Unmapped | Boolean | Strip all keys not explicitly mapped |

**Source path syntax:**

| Pattern | Description |
|---|---|
| `llm_response` | Top-level key |
| `http_response.data.id` | Nested property path |
| `parsed_output.items.0.name` | Array index access |
| `{{http_response.data.id}}` | Same as above, template braces are optional |

**Example mappings:**
```
customer_id   →  http_response.data.customer.id
display_name  →  parsed_output.name
fallback      →  {{default_text}}
```

---

### Filter

**Category:** Data  
**Color:** Green  
**Ports:** input (left), output (right)

Validates data against conditions. Can be configured to fail-fast (throw an error) or to pass through with a flag set.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Conditions | Key-Value JSON | Validation rules (key: condition expression) |
| Strict Mode | Boolean | Throw an error on validation failure |

---

### Chunk Text

**Category:** Data  
**Color:** Green  
**Ports:** input (left), output (right)

Splits a long text into smaller chunks for RAG pipelines. Supports character, word, and sentence chunking strategies.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Chunk Size | Number | Size of each chunk (characters/words/sentences depending on strategy) |
| Overlap | Number | How many units overlap between consecutive chunks |
| Strategy | Select | `Character` / `Word` / `Sentence` |

**Reads from WorkflowData:**

| Key | Description |
|---|---|
| `text` | The source text to chunk |
| `source` | Optional source label included in each chunk metadata |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `chunks` | `List<TextChunk>` — each chunk has `Text`, `Source`, `Index`, `StartPos`, `EndPos` |
| `chunk_count` | Number of chunks produced |

---

### Memory

**Category:** Data  
**Color:** Green  
**Ports:** input (left), output (right)

Reads from or writes to the workflow's global state memory. This memory persists across all nodes within a single workflow run (but not between separate runs).

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Mode | Select | `Read` or `Write` |
| Keys | String List | The WorkflowData keys to read into memory (Read mode) or write from memory (Write mode) |

**Read mode:** Copies the specified keys from global memory into the current WorkflowData.  
**Write mode:** Copies the specified keys from current WorkflowData into global memory.

**Use case:** Accumulating state between loop iterations, sharing data across parallel branches, storing user session values.

---

## 7. Node Reference — IO

IO nodes interact with external systems: HTTP APIs and files.

---

### HTTP Request

**Category:** IO  
**Color:** Purple  
**Ports:** input (left), output (right)

Makes HTTP requests to external REST APIs, webhooks, or data sources.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| Method | Select | `GET` / `POST` / `PUT` / `PATCH` / `DELETE` |
| URL Template | Text | URL with optional `{{variable}}` placeholders |
| Headers | Key-Value List | Request headers (e.g., `Authorization: Bearer {{api_token}}`) |
| Body | JSON | Request body for POST/PUT/PATCH (can be left empty to use `request_body` from WorkflowData) |
| Throw on Error | Boolean | Throw an exception for non-2xx responses (default: true) |
| Timeout (seconds) | Number | Request timeout (default: 30) |

**URL Template variable substitution:**  
Any `{{variable_name}}` in the URL is replaced with the value of that key from the current WorkflowData. Example:
```
https://api.example.com/customers/{{customer_id}}/orders
```
If `customer_id` is `"ABC123"` in WorkflowData, the URL becomes `https://api.example.com/customers/ABC123/orders`.

**Reads from WorkflowData:**

| Key | Description |
|---|---|
| `request_body` | Used as the POST/PUT body if no static Body is configured |
| `{{variable}}` | Any key referenced in the URL Template |

**Writes to WorkflowData:**

| Key | Description |
|---|---|
| `http_response` | Response body — parsed as a JSON object if the `Content-Type` is `application/json`, otherwise a raw string |
| `http_status_code` | HTTP status code (integer) |
| `http_headers` | Response headers dictionary |

---

## 8. Data Flow: WorkflowData

**WorkflowData** is the data packet that flows through the entire workflow. Each node receives a copy of the current data, reads from it, transforms it, and returns an updated copy to the next node.

Key concepts:

- **Keys are case-insensitive.** `LLM_Response` and `llm_response` are the same key.
- **Each node clones the data** before modifying it, so a failed node does not corrupt the previous state.
- **Keys accumulate** — each node adds its outputs without removing the keys written by earlier nodes, unless explicitly removed.
- **Type coercion** — data is stored as `object?` internally and coerced to the requested type. JSON-stored values are deserialized on read.

**Common keys flowing through an LLM pipeline:**

```
Start
  ↓ (initial data)
PromptBuilder
  ↓ writes: prompt, system_prompt
LLM
  ↓ writes: llm_response, llm_model, prompt_tokens, completion_tokens
OutputParser
  ↓ writes: parsed_output, + mapped fields
End
```

**WorkflowContext** is separate from WorkflowData:
- `WorkflowContext` is the run-scoped environment (logger, tracker, cancellation token, global state).
- GlobalState in context persists across nodes and iterations. Use the Memory node to read/write global state from the UI.
- WorkflowData is the per-step payload that flows node-to-node.

---

## 9. Sub-Workflows

Sub-workflows allow you to organize complex logic into reusable, named child workflows that are invoked from the parent using a **Sub Workflow** node.

### Creating a Sub-Workflow

1. Open the **Variables** tab in the left sidebar.
2. Under **Sub Workflows**, click **Add sub-workflow**.
3. A new child workflow is created with a default name.
4. Click **Open** (or **Open in new tab**) to start editing it.
5. Build the sub-workflow like any other workflow: Start → processing nodes → End.
6. Save.

### Invoking a Sub-Workflow

1. In the parent workflow canvas, drag a **Sub Workflow** node onto the canvas.
2. Click the node to open its properties.
3. In the **Target Sub-Workflow** dropdown, select the child workflow you created.
4. Connect the parent flow to the Sub Workflow node:
   ```
   [Previous Node] output ──► [Sub Workflow] input
   [Sub Workflow] success ──► [Next success node] input
   [Sub Workflow] error   ──► [Error handler node] input
   ```

### Data Passing

The parent's current WorkflowData is passed verbatim into the sub-workflow as its initial data. When the sub-workflow finishes, its final WorkflowData is returned to the parent on the `success` port.

### Sub-Workflow Error Handling

Each sub-workflow can have its own **Error Handler** node for internal error recovery. If the sub-workflow has no Error Handler (or the Error Handler's branch also fails), the failure propagates to the parent workflow, which routes execution to the `error` port of the invoking Sub Workflow node.

### URL Deep Links

Every sub-workflow has a unique URL:
```
/Workflow/Designer/{mainWorkflowId}?subWorkflowId={subWorkflowId}
```

Use the **Open in new tab** action from the Variables panel to get and use this link.

When in sub-workflow editing mode:
- The top toolbar shows a **Main Flow** button — click it to return to the parent workflow.
- The sub-workflow breadcrumb is shown to indicate which workflow you are currently editing.

---

## 10. Error Handling

The framework provides three layers of error handling:

### Layer 1 — Node-level (Execution Options)

Configure per-node behavior in the **Execution Options** section of the node properties panel:
- **Retry** — automatically retry on failure
- **Continue on Error** — allow the workflow to continue if this node fails
- **Fallback Data** — use a default data set if the node fails

See [Section 12 — Node Execution Options](#12-node-execution-options) for details.

### Layer 2 — ErrorNode (Workflow-level catch-all)

Drag an **Error Handler** node onto the canvas. It has no input connection — it acts as an automatic catch-all for any unhandled node failure in the workflow.

```
[Error Handler] ──► [Log "Workflow failed"] ──► [Notify Slack] ──► [End]
```

Rules:
- Maximum one Error Handler per workflow (and one per sub-workflow).
- It receives the WorkflowData state at the point of failure.
- If no Error Handler exists and a node fails, the workflow stops and reports the error.

### Layer 3 — Sub-Workflow error port

When using a **Sub Workflow** node, connect its `error` output port to a recovery path:

```
[Sub Workflow] ──► success ──► [Continue flow]
               └──► error  ──► [Log error] ──► [Fallback response] ──► [End]
```

This lets the parent workflow handle failures in child workflows gracefully without an unhandled exception.

---

## 11. Data Input Mapping

For nodes with input ports, the **Data Input** section of the properties panel lets you define an **Input Mapping (JSON)**.

This remaps data from the current WorkflowData into the specific key names that the node expects — without needing a separate Data Mapper node.

**Format:**
```json
{
  "target_key": "source_path"
}
```

**Source path patterns:**

| Pattern | Description |
|---|---|
| `prev.llm_response` | A key from the previous node's output (shorthand) |
| `http_response.data.id` | Nested path in a JSON response |
| `@context.tenantId` | A value from the WorkflowContext global state |
| `{{variable_name}}` | A workflow variable defined in the Variables tab |
| `"literal value"` | A static string literal |

**Example — passing data into an LLM node:**
```json
{
  "prompt": "prev.user_question",
  "system_prompt": "{{base_system_prompt}}",
  "customer_id": "http_response.data.id",
  "tenant": "@context.tenantId"
}
```

**When to use Input Mapping vs Data Mapper node:**

| Scenario | Use |
|---|---|
| Simple remapping for a single downstream node | Input Mapping in the node properties |
| Complex or reusable mapping shared across multiple nodes | Data Mapper node |
| Removing unmapped keys from the data bag | Data Mapper node with Remove Unmapped enabled |
| Nested path resolution with default fallbacks | Data Mapper node with Default Values |

---

## 12. Node Execution Options

Every node supports execution options, configured in the **Execution Options** section of the properties panel.

### Retry

| Option | Type | Description |
|---|---|---|
| Max Retries | Number | Number of additional attempts after first failure (0 = no retry) |
| Retry Delay (ms) | Number | Base delay between retries. Uses exponential backoff: `delay × 2^attempt` |

**Example:** Max Retries = 3, Retry Delay = 1000ms → retries at 1s, 2s, 4s.

Use retry on nodes that call external APIs or LLMs where transient failures are expected.

### Timeout

| Option | Type | Description |
|---|---|---|
| Timeout (ms) | Number | Maximum time this node is allowed to run. The node is cancelled if exceeded. |

### Continue on Error

| Option | Type | Description |
|---|---|---|
| Continue on Error | Boolean | If true, a failure in this node does not stop the workflow. Execution continues with the last successful WorkflowData. |
| Fallback Data | JSON | Optional data to inject if the node fails (used together with Continue on Error) |

### Run Condition

| Option | Type | Description |
|---|---|---|
| Run Condition | Text | Expression that must evaluate to true for this node to execute. If false, the node is skipped and data passes through unchanged. |

**Example condition:** `{{feature_enabled}} == true` — skips this node if the `feature_enabled` workflow variable is not `true`.

---

## 13. Variables

Workflow variables are defined in the **Variables** tab of the left sidebar. They are key-value pairs that are available across the entire workflow and all its sub-workflows.

**To add a variable:**
1. Open the Variables tab.
2. Click **Add Variable**.
3. Enter a name and value.
4. Save the workflow.

**To use a variable in node parameters:**
```
{{variable_name}}
```

Variables resolve in:
- Node parameter fields
- Prompt templates in Prompt Builder
- URL Templates in HTTP Request
- Input Mapping JSON
- Run Condition expressions

**Variable resolution order:**
1. WorkflowData key with that name (checked first)
2. Workflow variable (checked second)
3. Unresolved → `{{MISSING:variable_name}}`

---

## 14. Common Workflow Patterns

### Pattern 1: Simple LLM Q&A

```
Start ──► PromptBuilder ──► LLM ──► End
```

1. **PromptBuilder** — Template: `Answer this question clearly: {{question}}`
2. **LLM** — Model: `gpt-4o`, Temperature: 0.3
3. Result is in `llm_response`

---

### Pattern 2: Structured Output Extraction

```
Start ──► PromptBuilder ──► LLM ──► OutputParser ──► End
```

1. **PromptBuilder** — Prompt instructs the LLM to return JSON
2. **LLM** — Call the model
3. **OutputParser** — Field Mapping: `{ "sentiment": "sentiment_label", "score": "confidence" }`

---

### Pattern 3: Conditional Routing

```
Start ──► PromptBuilder ──► LLM ──► OutputParser ──► Condition ──► [true branch] ──► End
                                                                └──► [false branch] ──► End
```

1. **Condition** — Evaluates `confidence > 0.8` → writes `is_high_confidence: true/false`
2. Connect `output` to a Branch-style manual port check, or use the Condition result directly in Input Mappings

---

### Pattern 4: HTTP API + LLM Augmentation

```
Start ──► HTTP Request ──► DataMapper ──► PromptBuilder ──► LLM ──► End
```

1. **HTTP Request** — GET customer data from API, writes `http_response`
2. **DataMapper** — Maps `http_response.data.name` → `customer_name`, etc.
3. **PromptBuilder** — Injects `customer_name` into prompt template
4. **LLM** — Generates personalised response

---

### Pattern 5: Sub-Workflow Composition

```
Main:     Start ──► [Validate Input] ──► [Sub Workflow: Process Order] ──► success ──► End
                                                                        └──► error  ──► [Log Error] ──► End

Sub:      Start ──► [HTTP: Fetch Order] ──► [LLM: Summarise] ──► [HTTP: Update Status] ──► End
          [Error Handler] ──► [Log] ──► End
```

1. Build the sub-workflow independently with its own Error Handler.
2. In the main workflow, use a Sub Workflow node targeting it.
3. Handle `success` and `error` ports in the parent.

---

### Pattern 6: RAG Pipeline

```
Start ──► FileReader ──► ChunkText ──► Loop(ForEach) ──► Embedding ──► End
                                              │
                                           [chunk item]
                                              │
                                           Embedding
```

1. **FileReader** — reads a document into `text`
2. **ChunkText** — splits into `chunks` (List)
3. **Loop (ForEach)** — iterates over `chunks`, runs Embedding for each
4. **Embedding** — generates `embedding` per chunk
5. Collected results go into `embeddings`

---

### Pattern 7: Multi-Step Branch (Switch)

```
Start ──► Branch ──► case1 ──► [Handle: complaint]    ──► End
                 ├──► case2 ──► [Handle: billing]      ──► End
                 ├──► case3 ──► [Handle: technical]    ──► End
                 └──► default ──► [Handle: general]    ──► End
```

1. **Branch** — Value Key: `intent`, Case 1: `complaint`, Case 2: `billing`, Case 3: `technical`
2. Each route handles the specific intent independently.

---

### Pattern 8: Retry with Fallback

For an unreliable external API:

1. **HTTP Request** node
2. Set **Max Retries** = 3, **Retry Delay** = 2000ms
3. Set **Continue on Error** = true
4. Set **Fallback Data** = `{ "http_response": { "status": "unavailable" } }`
5. Connect the output to an **ErrorRoute** Condition that checks for the fallback status.

This ensures the workflow never hard-fails on a flaky API and always produces a usable result.

## 7. Connection Rules and Tips

- Connect outputs to compatible inputs
- Keep one clear forward direction on canvas
- Use `Condition`, `Branch`, and `Sub Workflow` nodes for split paths
- Keep success and error paths visually separated

## 8. Save and Persistence

- Use `Save` after structure or parameter changes
- New sub-workflow creation triggers auto-save, but you should still manually save after major edits
- Workflow JSON stores:
  - main workflow nodes/connections/variables
  - child sub-workflows
  - node parameters including `inputMapping`

## 9. Keyboard Shortcuts

- `Delete`: delete selected node/connection
- `Esc`: clear selection
- `Ctrl+S`: save workflow
- `Ctrl+A`: select all nodes

## 10. Suggested Authoring Pattern

1. Design main happy-path flow
2. Extract reusable steps into sub-workflows
3. Connect with `Sub Workflow` nodes
4. Add `ErrorNode` branch for each workflow scope
5. Add `Data Input` mappings on nodes that consume structured inputs
6. Save and test incrementally

