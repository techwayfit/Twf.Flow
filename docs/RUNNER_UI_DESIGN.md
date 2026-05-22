# Runner View UI — Design Document

## 1. Overview

The **Runner View** is a read-only companion to the Workflow Designer that lets you:

- Collect inputs before execution
- Watch each node light up as it executes (green = running, blue = done, red = failed)
- See the exact input and output data for every node in a live step panel
- Navigate back to the designer with one click

The runner reuses the full existing `WorkflowDefinitionRunner` engine and the existing canvas rendering pipeline. No new AI / ML dependencies are needed.

---

## 2. Architecture

```
Browser                          ASP.NET Core
──────────────────────────────   ──────────────────────────────────────────
GET /Workflow/Runner/{id}   ──►  WorkflowController.Runner()
                            ◄──  Runner.cshtml  (read-only canvas)

[User fills inputs, clicks Run]
POST /Workflow/RunStream/{id} ─► WorkflowController.RunStream()
text/event-stream             ◄─ Server-Sent Events (SSE) per node + final result
                                  └── WorkflowDefinitionRunner (modified)
```

### URL plan

| Method | URL | Purpose |
|--------|-----|---------|
| `GET`  | `/Workflow/Runner/{id}` | Renders the Runner view |
| `POST` | `/Workflow/RunStream/{id}` | SSE endpoint — streams per-node events and final result |
| `POST` | `/Workflow/Run/{id}` *(existing)* | Single-shot JSON result (kept as-is) |

---

## 3. Component Inventory

### New files to create

| File | Role |
|------|------|
| `source/web/Views/Workflow/Runner.cshtml` | Razor view — read-only canvas + input modal + step panel |
| `source/web/wwwroot/js/runner.js` | All runner-specific JS (SSE, node highlighting, step panel) |
| `source/web/wwwroot/css/runner.css` | Runner-specific CSS (node state colours, step panel layout) |

### Files to modify

| File | Change |
|------|--------|
| `source/web/Controllers/WorkflowController.cs` | Add `Runner` GET action + `RunStream` POST+SSE action |
| `source/web/Services/WorkflowDefinitionRunner.cs` | Add `RunWithCallbackAsync` overload that fires a delegate after each node |
| `source/web/Views/Workflow/Designer.cshtml` | Add **Run** button in toolbar |
| `source/web/Models/WorkflowRunResult.cs` | Add `NodeStepEvent` record |

---

## 4. Server-Side Changes

### 4.1 NodeStepEvent model

Add to `source/web/Models/WorkflowRunResult.cs`:

```csharp
/// <summary>
/// Emitted by WorkflowDefinitionRunner after each node executes.
/// Serialised as a Server-Sent Event payload.
/// </summary>
public record NodeStepEvent(
    string      EventType,   // "node_start" | "node_done" | "node_error" | "workflow_done" | "workflow_error"
    Guid        NodeId,
    string      NodeName,
    string      NodeType,
    Dictionary<string, object?> InputData,
    Dictionary<string, object?> OutputData,
    string|     ErrorMessage,
    DateTimeOffset Timestamp
);
```

### 4.2 WorkflowDefinitionRunner — streaming overload

Add a second public method alongside the existing `RunAsync`. The signature accepts a callback that the graph-walker fires **before** and **after** each node:

```csharp
public async Task<WorkflowRunResult> RunWithCallbackAsync(
    WorkflowDefinition definition,
    WorkflowData? initialData,
    Func<NodeStepEvent, Task> onStep)        // ← new parameter
```

Inside `WalkGraphAsync`, wrap each real node execution with two callback calls:

```csharp
// Before execution
await onStep(new NodeStepEvent(
    EventType:   "node_start",
    NodeId:      nodeDef.Id,
    NodeName:    nodeDef.Name,
    NodeType:    nodeDef.Type,
    InputData:   data.ToDictionary(),
    OutputData:  new(),
    ErrorMessage: null,
    Timestamp:   DateTimeOffset.UtcNow));

NodeResult result;
try
{
    result = await node.ExecuteWithOptionsAsync(data, context, options);
}
catch (Exception ex)
{
    await onStep(new NodeStepEvent("node_error", nodeDef.Id, nodeDef.Name,
        nodeDef.Type, data.ToDictionary(), new(), ex.Message, DateTimeOffset.UtcNow));
    throw;
}

// After execution
await onStep(new NodeStepEvent(
    EventType:   "node_done",
    NodeId:      nodeDef.Id,
    NodeName:    nodeDef.Name,
    NodeType:    nodeDef.Type,
    InputData:   data.ToDictionary(),           // data before the node
    OutputData:  result.Data?.ToDictionary() new(),
    ErrorMessage: null,
    Timestamp:   DateTimeOffset.UtcNow));
```

The existing `RunAsync` simply delegates to `RunWithCallbackAsync` with a no-op callback:

```csharp
public Task<WorkflowRunResult> RunAsync(
    WorkflowDefinition definition,
    WorkflowData? initialData = null)
    => RunWithCallbackAsync(definition, initialData, _ => Task.CompletedTask);
```

### 4.3 WorkflowController — Runner GET action

```csharp
// GET: /Workflow/Runner/{id}
public async Task<IActionResult> Runner(Guid id)
{
    var workflow = await _repository.GetByIdAsync(id);
    if (workflow == null) return NotFound();
    return View(workflow);
}
```

### 4.4 WorkflowController — RunStream SSE action

```csharp
// POST: /Workflow/RunStream/{id}
// Content-Type response: text/event-stream
[HttpPost]
public async Task RunStream(Guid id, [FromBody] WorkflowRunRequest? request)
{
    var workflow = await _repository.GetByIdAsync(id);
    if (workflow == null)
    {
        Response.StatusCode = 404;
        return;
    }

    Response.Headers["Content-Type"]  = "text/event-stream";
    Response.Headers["Cache-Control"] = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no"; // disable Nginx buffering

    var initialData = request?.InitialData is { Count: > 0 } d
        ? new WorkflowData(d)
        : null;

    async Task WriteEvent(string eventName, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    try
    {
        var result = await _runner.RunWithCallbackAsync(
            workflow,
            initialData,
            async step =>
            {
                await WriteEvent(step.EventType, step);
            });

        var finalEvent = result.IsSuccess ? "workflow_done" : "workflow_error";
        await WriteEvent(finalEvent, result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled error streaming workflow {Id}", id);
        await WriteEvent("workflow_error",
            WorkflowRunResult.Failure(workflow.Name, new WorkflowData(), ex.Message, null));
    }
}
```

---

## 5. Designer Toolbar — "Run" Button

In `source/web/Views/Workflow/Designer.cshtml`, add one button after the **Save** button:

```html
<a href="@Url.Action("Runner", new { id = Model.Id })" class="btn-toolbar btn-run" target="_blank">
    <i class="bi bi-play-circle-fill"></i> Run
</a>
```

The `target="_blank"` opens the runner in a new tab so the designer state is preserved.

---

## 6. Runner.cshtml Layout

The runner mirrors the designer's full-screen layout but replaces the left sidebar and properties panel with an **Execution Log** panel.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TOOLBAR: [← Back to Designer] [workflow name]       [▶ Run]  [✕ Stop] │
├────────────────────────────────────┬────────────────────────────────────┤
│                                    │  STEP PANEL                        │
│   READ-ONLY CANVAS                 │  ┌──────────────────────────────┐  │
│   (nodes rendered, no drag/edit)   │  │ ▶ StartNode          ✓ done │  │
│                                    │  │   ─────────────────────────  │  │
│   Nodes glow as they execute →     │  │   Input:  {}                │  │
│   ○ grey   = pending               │  │   Output: {}                │  │
│   ◉ yellow = running               │  ├──────────────────────────────┤  │
│   ● green  = done                  │  │ ⏳ LlmNode            running│  │
│   ● red    = failed                │  │   ─────────────────────────  │  │
│                                    │  │   Input:  { prompt: "..." } │  │
│                                    │  │   Output: (waiting...)      │  │
│                                    │  └──────────────────────────────┘  │
└────────────────────────────────────┴────────────────────────────────────┘
```

Key structural decisions:
- **No left sidebar** — the node palette is irrelevant; the canvas is wider
- **Read-only canvas** — same rendering pipeline, but all mouse event listeners are not attached
- **Right step panel** — scrollable list of node execution cards, in execution order
- **Input modal** — shown before the user clicks Run, collects key-value pairs for `InitialData`

### 6.1 Skeleton HTML structure

```html
@model Twf.Flow.Web.Models.WorkflowDefinition
@{ Layout = null; }
<!DOCTYPE html>
<html lang="en">
<head>
  ... (same CSS links as Designer.cshtml, plus runner.css)
</head>
<body>
  <div id="runner-container">

    <!-- Toolbar -->
    <div id="runner-toolbar">
      <div>
        <a href="@Url.Action("Designer", new { id = Model.Id })" class="btn-toolbar">
          <i class="bi bi-pencil-square"></i> Edit in Designer
        </a>
        <h5><i class="bi bi-play-circle-fill"></i> <span id="workflow-name">@Model.Name</span></h5>
      </div>
      <div id="toolbar-buttons">
        <span id="run-status-badge" class="badge bg-secondary">Ready</span>
        <button class="btn-toolbar btn-success" onclick="showInputModal()">
          <i class="bi bi-play-fill"></i> Run
        </button>
        <button class="btn-toolbar btn-danger" id="btn-stop" onclick="stopRun()" disabled>
          <i class="bi bi-stop-fill"></i> Stop
        </button>
      </div>
    </div>

    <!-- Main area: canvas + step panel -->
    <div id="runner-main-area">

      <!-- Read-only canvas (same structure as designer) -->
      <div id="canvas-area">
        <svg id="workflow-canvas" xmlns="http://www.w3.org/2000/svg">
          <defs>... (same arrowhead markers)</defs>
          <g id="connections-layer"></g>
        </svg>
        <div id="nodes-layer"></div>
      </div>

      <!-- Step panel -->
      <div id="step-panel">
        <h6><i class="bi bi-list-check"></i> Execution Log</h6>
        <div id="step-cards"></div>
      </div>

    </div>
  </div>

  <!-- Input modal -->
  <div id="input-modal" class="modal-overlay" style="display:none;">
    <div class="modal-box">
      <h5><i class="bi bi-input-cursor-text"></i> Workflow Inputs</h5>
      <p class="text-muted small">Provide initial data for the workflow. Leave empty to use no inputs.</p>
      <div id="input-fields">
        <!-- Dynamically generated key-value rows -->
      </div>
      <button class="btn btn-link btn-sm" onclick="addInputField()">
        <i class="bi bi-plus-circle"></i> Add field
      </button>
      <div class="modal-footer-btns">
        <button class="btn btn-secondary btn-sm" onclick="closeInputModal()">Cancel</button>
        <button class="btn btn-success btn-sm" onclick="startRun()">
          <i class="bi bi-play-fill"></i> Run Workflow
        </button>
      </div>
    </div>
  </div>

  <!-- Scripts: all the same designer JS except events.js (no drag/edit) -->
  ... (same script tags as Designer.cshtml minus events.js write handlers)

  <script src="~/js/runner.js" asp-append-version="true"></script>
  <script>
    const workflowId  = '@Model.Id';
    const workflowDef = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model));
  </script>
</body>
</html>
```

---

## 7. runner.js — Client-Side Logic

### 7.1 Initialisation

```javascript
// runner.js
let runnerEventSource = null;
let nodeStates = {};   // nodeId → "pending" | "running" | "done" | "error"

document.addEventListener('DOMContentLoaded', () => {
    // 1. Populate window.workflow from the server-injected workflowDef
    window.workflow = workflowDef;

    // 2. Render canvas (read-only — call render() but skip event binding)
    renderReadOnly();
});

function renderReadOnly() {
    // Reuse existing renderNodes() and renderConnections() —
    // they read from window.workflow and paint to #nodes-layer / #connections-layer.
    // Because we never call setupEventListeners(), nodes aren't draggable.
    render();
}
```

### 7.2 Input modal

```javascript
function showInputModal() {
    document.getElementById('input-modal').style.display = 'flex';
}

function closeInputModal() {
    document.getElementById('input-modal').style.display = 'none';
}

function addInputField() {
    const row = document.createElement('div');
    row.className = 'input-row';
    row.innerHTML = `
        <input type="text" placeholder="key"   class="form-control form-control-sm input-key" />
        <input type="text" placeholder="value" class="form-control form-control-sm input-value" />
        <button class="btn btn-sm btn-outline-danger" onclick="this.closest('.input-row').remove()">✕</button>`;
    document.getElementById('input-fields').appendChild(row);
}

function collectInputs() {
    const inputs = {};
    document.querySelectorAll('#input-fields .input-row').forEach(row => {
        const key = row.querySelector('.input-key').value.trim();
        const val = row.querySelector('.input-value').value;
        if (key) inputs[key] = val;
    });
    return inputs;
}
```

### 7.3 SSE-driven execution

```javascript
function startRun() {
    closeInputModal();
    resetRunnerState();

    const initialData = collectInputs();

    // Open SSE stream via POST (fetch API supports streaming response)
    runnerEventSource = null;

    fetch(`/Workflow/RunStream/${workflowId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ initialData })
    }).then(response => {
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        function pump() {
            reader.read().then(({ done, value }) => {
                if (done) { onStreamDone(); return; }
                buffer += decoder.decode(value, { stream: true });
                const events = buffer.split('\n\n');
                buffer = events.pop(); // keep incomplete event
                events.forEach(parseSSEChunk);
                pump();
            });
        }
        pump();
    });

    updateStatusBadge('running');
    document.getElementById('btn-stop').disabled = false;
}

function parseSSEChunk(chunk) {
    const lines = chunk.split('\n');
    let eventType = '';
    let data = '';
    lines.forEach(line => {
        if (line.startsWith('event: ')) eventType = line.slice(7).trim();
        if (line.startsWith('data: '))  data      = line.slice(6).trim();
    });
    if (!eventType || !data) return;
    const payload = JSON.parse(data);
    handleRunnerEvent(eventType, payload);
}

function handleRunnerEvent(eventType, payload) {
    switch (eventType) {
        case 'node_start':
            setNodeState(payload.nodeId, 'running');
            addStepCard(payload, 'running');
            break;
        case 'node_done':
            setNodeState(payload.nodeId, 'done');
            updateStepCard(payload.nodeId, 'done', payload);
            break;
        case 'node_error':
            setNodeState(payload.nodeId, 'error');
            updateStepCard(payload.nodeId, 'error', payload);
            break;
        case 'workflow_done':
            updateStatusBadge('done');
            document.getElementById('btn-stop').disabled = true;
            break;
        case 'workflow_error':
            updateStatusBadge('error');
            document.getElementById('btn-stop').disabled = true;
            break;
    }
}

function stopRun() {
    if (runnerEventSource) runnerEventSource.cancel();
    updateStatusBadge('stopped');
    document.getElementById('btn-stop').disabled = true;
}

function resetRunnerState() {
    nodeStates = {};
    document.getElementById('step-cards').innerHTML = '';
    document.querySelectorAll('.workflow-node').forEach(el => {
        el.classList.remove('node-running', 'node-done', 'node-error', 'node-pending');
        el.classList.add('node-pending');
    });
}
```

### 7.4 Node highlighting

```javascript
function setNodeState(nodeId, state) {
    nodeStates[nodeId] = state;
    const el = document.querySelector(`[data-node-id="${nodeId}"]`);
    if (!el) return;
    el.classList.remove('node-running', 'node-done', 'node-error', 'node-pending');
    el.classList.add(`node-${state}`);
    if (state === 'running') scrollNodeIntoView(el);
}

function scrollNodeIntoView(el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}
```

### 7.5 Step panel cards

```javascript
function addStepCard(step, state) {
    const card = document.createElement('div');
    card.className = `step-card step-${state}`;
    card.id        = `step-card-${step.nodeId}`;
    card.innerHTML = `
        <div class="step-header">
            <span class="step-icon">${stateIcon(state)}</span>
            <strong>${step.nodeName}</strong>
            <small class="text-muted ms-1">${step.nodeType}</small>
            <span class="step-time ms-auto">${new Date(step.timestamp).toLocaleTimeString()}</span>
        </div>
        <div class="step-body" style="display:none;">
            <div class="step-section"><b>Input</b><pre>${JSON.stringify(step.inputData, null, 2)}</pre></div>
            <div class="step-section"><b>Output</b><pre id="step-output-${step.nodeId}">(waiting…)</pre></div>
        </div>`;
    card.querySelector('.step-header').addEventListener('click', () => {
        const body = card.querySelector('.step-body');
        body.style.display = body.style.display === 'none' ? 'block' : 'none';
    });
    document.getElementById('step-cards').appendChild(card);
    card.scrollIntoView({ behavior: 'smooth', block: 'end' });
}

function updateStepCard(nodeId, state, step) {
    const card = document.getElementById(`step-card-${nodeId}`);
    if (!card) return;
    card.className = `step-card step-${state}`;
    card.querySelector('.step-icon').textContent = stateIcon(state);
    const outputEl = document.getElementById(`step-output-${nodeId}`);
    if (outputEl) {
        outputEl.textContent = step.errorMessage
            ? `ERROR: ${step.errorMessage}`
            : JSON.stringify(step.outputData, null, 2);
    }
    // Auto-expand on error
    if (state === 'error') {
        card.querySelector('.step-body').style.display = 'block';
    }
}

function stateIcon(state) {
    return { running: '⏳', done: '✅', error: '❌', pending: '○' }[state] '○';
}

function updateStatusBadge(state) {
    const badge = document.getElementById('run-status-badge');
    const map = {
        ready:   ['bg-secondary', 'Ready'],
        running: ['bg-warning text-dark', 'Running…'],
        done:    ['bg-success', 'Completed'],
        error:   ['bg-danger', 'Failed'],
        stopped: ['bg-dark', 'Stopped'],
    };
    const [cls, label] = map[state] || map.ready;
    badge.className = `badge ${cls}`;
    badge.textContent = label;
}
```

---

## 8. runner.css — Node State Styles

```css
/* runner.css */

/* Base — all nodes dimmed when a run is active */
.runner-active .workflow-node {
    opacity: 0.4;
    transition: opacity 0.3s ease, box-shadow 0.3s ease, border-color 0.3s ease;
}

/* Pending (not yet reached) */
.workflow-node.node-pending {
    opacity: 0.4;
}

/* Running — bright yellow pulse */
.workflow-node.node-running {
    opacity: 1;
    border-color: #f39c12 !important;
    box-shadow: 0 0 0 4px rgba(243, 156, 18, 0.5),
                0 0 16px 4px rgba(243, 156, 18, 0.4);
    animation: node-pulse 0.8s ease-in-out infinite alternate;
}

@keyframes node-pulse {
    from { box-shadow: 0 0 0 3px rgba(243,156,18,0.4), 0 0 10px rgba(243,156,18,0.3); }
    to   { box-shadow: 0 0 0 6px rgba(243,156,18,0.7), 0 0 20px rgba(243,156,18,0.6); }
}

/* Done — green */
.workflow-node.node-done {
    opacity: 1;
    border-color: #27ae60 !important;
    box-shadow: 0 0 0 3px rgba(39, 174, 96, 0.4);
}

/* Error — red */
.workflow-node.node-error {
    opacity: 1;
    border-color: #e74c3c !important;
    box-shadow: 0 0 0 3px rgba(231, 76, 60, 0.5),
                0 0 10px rgba(231, 76, 60, 0.4);
}

/* ── Step panel ── */
#runner-container {
    display: flex;
    flex-direction: column;
    height: 100vh;
    overflow: hidden;
}

#runner-main-area {
    display: flex;
    flex: 1;
    overflow: hidden;
}

#canvas-area {
    flex: 1;
    position: relative;
    overflow: hidden;
    background: #f8f9fa;
}

#step-panel {
    width: 340px;
    min-width: 280px;
    display: flex;
    flex-direction: column;
    border-left: 1px solid #dee2e6;
    background: #fff;
    overflow: hidden;
}

#step-panel h6 {
    padding: 12px 16px;
    margin: 0;
    border-bottom: 1px solid #dee2e6;
    font-weight: 600;
    color: #495057;
}

#step-cards {
    flex: 1;
    overflow-y: auto;
    padding: 8px;
}

/* Step cards */
.step-card {
    border: 1px solid #dee2e6;
    border-radius: 6px;
    margin-bottom: 6px;
    overflow: hidden;
    transition: border-color 0.2s ease;
}

.step-card.step-running { border-color: #f39c12; }
.step-card.step-done    { border-color: #27ae60; }
.step-card.step-error   { border-color: #e74c3c; }

.step-header {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 8px 10px;
    cursor: pointer;
    user-select: none;
    background: #fafafa;
}

.step-header:hover { background: #f0f0f0; }

.step-body {
    padding: 8px 12px;
    border-top: 1px solid #eee;
    font-size: 0.8rem;
}

.step-body pre {
    background: #f8f9fa;
    border: 1px solid #e9ecef;
    border-radius: 4px;
    padding: 8px;
    font-size: 0.75rem;
    max-height: 200px;
    overflow-y: auto;
    white-space: pre-wrap;
    word-break: break-all;
}

.step-section { margin-bottom: 8px; }
.step-section b { display: block; margin-bottom: 4px; color: #495057; }

/* Input modal */
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
}

.modal-box {
    background: #fff;
    border-radius: 8px;
    padding: 24px;
    width: 500px;
    max-width: 90vw;
    max-height: 80vh;
    overflow-y: auto;
    box-shadow: 0 8px 32px rgba(0,0,0,0.2);
}

.input-row {
    display: grid;
    grid-template-columns: 1fr 1fr auto;
    gap: 6px;
    margin-bottom: 8px;
    align-items: center;
}

.modal-footer-btns {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 16px;
    padding-top: 12px;
    border-top: 1px solid #dee2e6;
}

/* Runner toolbar */
#runner-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 16px;
    background: #2c3e50;
    color: #fff;
    gap: 12px;
    border-bottom: 3px solid #27ae60;
}

#runner-toolbar h5 {
    margin: 0;
    font-size: 1rem;
    color: #ecf0f1;
}
```

---

## 9. WorkflowData Helper — `ToDictionary()`

The `NodeStepEvent` emits `InputData` and `OutputData` as `Dictionary<string, object?>`. Add this helper to the core `WorkflowData` class if it does not exist:

```csharp
// In Twf.Flow.Core.WorkflowData (or as an extension in the web project)
public Dictionary<string, object?> ToDictionary()
    => new Dictionary<string, object?>(_store);
```

If adding to core is not desirable, add a private helper to `WorkflowDefinitionRunner`:

```csharp
private static Dictionary<string, object?> Snapshot(WorkflowData data)
    => data.Keys.ToDictionary(k => k, k => data.Get<object?>(k));
```

---

## 10. Implementation Phases

### Phase 1 — Toolbar button (30 min)
- Add **Run** button/link to `Designer.cshtml` toolbar pointing to `/Workflow/Runner/{id}`
- No other changes needed; the Runner view doesn't exist yet, so it 404s — acceptable until Phase 2

### Phase 2 — Runner view skeleton (2–3 h)
- Create `Runner.cshtml` with read-only canvas, toolbar, and step panel structure
- Add `GET /Workflow/Runner/{id}` action to `WorkflowController`
- Create `runner.css` and `runner.js` with canvas initialisation only (no run logic yet)
- Verify the canvas renders the workflow correctly in read-only mode

### Phase 3 — SSE streaming (2–3 h)
- Add `NodeStepEvent` model
- Add `RunWithCallbackAsync` overload to `WorkflowDefinitionRunner` with pre/post node callbacks
- Add `POST /Workflow/RunStream/{id}` SSE endpoint to `WorkflowController`
- In `runner.js`, implement `startRun()` → `fetch` → SSE reader → `handleRunnerEvent()`
- Test with a simple 3-node workflow; verify SSE events arrive in order

### Phase 4 — Node highlighting (1 h)
- Add CSS classes `node-running`, `node-done`, `node-error`, `node-pending` to `runner.css`
- Wire `setNodeState()` in `runner.js` to `handleRunnerEvent()`
- Add the `.runner-active` class to `#runner-container` when a run starts

### Phase 5 — Input modal + step panel (1–2 h)
- Build the input modal HTML and `addInputField()` / `collectInputs()` logic
- Build step card creation (`addStepCard`) and update (`updateStepCard`) functions
- Auto-scroll step cards into view as they appear
- Auto-expand error cards

### Phase 6 — Polish (1 h)
- Status badge labels and colour transitions
- Stop button cancels the fetch reader mid-stream
- Handle the case where the browser is closed mid-run (server-side: catch `OperationCanceledException` on `HttpContext.RequestAborted`)
- Resize handle between canvas and step panel (optional)

---

## 11. Security Considerations

| Risk | Mitigation |
|------|-----------|
| Arbitrary `initialData` injection | SSE endpoint reads data from the authenticated request body; existing `[ValidateAntiForgeryToken]` pattern should be applied, or use a bearer token if the project adopts one |
| Workflow content is server-serialised into the Razor view | `Html.Raw(JsonSerializer.Serialize(Model))` is safe as long as the JSON does not contain `</script>` — use `JsonSerializerOptions` with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` disabled (the default encoder escapes `<`, `>`, `&`) |
| SSE keeps a connection open indefinitely | Use `HttpContext.RequestAborted` as a `CancellationToken` in `RunWithCallbackAsync` and propagate it to `node.ExecuteWithOptionsAsync` |

---

## 12. Open Questions / Future Enhancements

| Topic | Notes |
|-------|-------|
| **SignalR vs SSE** | SSE is simpler (no extra package, uni-directional, works over HTTP/2). SignalR would add bi-directional control but is heavier. SSE is the recommended starting point. |
| **Sub-workflow drill-down** | When a `SubWorkflowNode` executes, its inner nodes should also highlight. The SSE callback fires for every node in `WalkGraphAsync`, including recursive calls — the `Runner.cshtml` could show a collapsible "sub-flow" section when it encounters `node_start` events for nodes not in the main canvas. |
| **Parallel nodes** | `ParallelNode` runs branches concurrently. The callback will fire interleaved events — the step panel should group them under a parent `ParallelNode` card. |
| **Loop iterations** | The `LoopNode` repeats nodes. Consider emitting an `iteration` field in the event so the step panel shows Iteration 1 / 2 / 3 cards rather than repeating the same card. |
| **Persisting run results** | Run results could be saved to a `WorkflowRun` table (SQLite) for history / replay. |
| **Dark mode** | The runner CSS can adapt to Bootstrap's `data-bs-theme="dark"` attribute. |
