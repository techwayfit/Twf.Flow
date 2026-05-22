# Creating a New Node

This guide walks through adding a new node from scratch. After the refactor, there are exactly **four places** to touch — no switch-cases to update, no frontend parameter maps to maintain.

---

## Overview of the node system

```
source/core/Nodes/<Category>/<YourNode>.cs   ← execution logic + schema + palette metadata
source/web/Services/NodeTypeSeeder.cs        ← node palette entry (name, icon, colour)
source/web/designer-react/src/nodeConfig.js  ← routing ports only (if non-standard)
```

Everything lives in the node file itself:
- **Execution logic** — `RunAsync`
- **Data ports** — `DataIn` / `DataOut`
- **Properties panel form** — `static NodeParameterSchema Schema { get; }`

The workflow runner and the schema provider both discover nodes **via reflection**. As long as your node class exists in the core assembly, it will be picked up automatically.

---

## Step 1 — Implement the node class

Create a sealed class in `source/core/Nodes/<Category>/` inheriting from `BaseNode`.

### Minimal template

```csharp
using Twf.Flow.Core;
using Twf.Flow.Nodes;

namespace Twf.Flow.Nodes.Data;   // adjust namespace to match category

/// <summary>
/// One-line description of what this node does.
///
/// Reads from WorkflowData:
///   - "input_key" : description of what it expects
///
/// Writes to WorkflowData:
///   - "output_key" : description of what it produces
/// </summary>
public sealed class MyCustomNode : BaseNode
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public override string Name { get; }
    public override string Category => "Data";   // AI | Control | Data | IO
    public override string Description => "One-line description shown in the runner log";
    public override string IdPrefix => "mycustom";   // "mycustom001", "mycustom002", …

    // ── Data ports ────────────────────────────────────────────────────────────
    // Every key listed here maps to a WorkflowData key.
    // Required=true → the runner will fail with a clear error if the key is absent.

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new("input_key", typeof(string), Required: true,  "Description shown in the UI"),
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new("output_key", typeof(string), Description: "Description shown in the UI"),
    ];

    // ── Configuration ─────────────────────────────────────────────────────────

    private readonly string _myParam;

    // Standard constructor — used by code-first workflows and tests
    public MyCustomNode(string name, string myParam)
    {
        Name     = name;
        _myParam = myParam;
    }

    // Dictionary constructor — REQUIRED for the runner to instantiate this node
    // dynamically. Parameter keys must match the names in NodeSchemaProvider.
    public MyCustomNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")    "My Custom Node",
            NodeParameters.GetString(parameters, "myParam") "default value")
    { }

    // ── Execution ─────────────────────────────────────────────────────────────

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var value = input.GetRequiredString("input_key");

        nodeCtx.Log($"Processing with param '{_myParam}': {value[..Math.Min(80, value.Length)]}");

        var result = $"{_myParam}: {value}";  // replace with your logic

        nodeCtx.SetMetadata("my_custom_meta", result.Length);

        return Task.FromResult(
            input.Clone().Set("output_key", result));
    }
}
```

### Key rules

| Rule | Why |
|---|---|
| `sealed class` | Prevents inheritance surprises. |
| `IdPrefix` lowercase letters only | Used to generate `mycustom001`, `mycustom002` etc. |
| `DataIn` / `DataOut` declared as properties | The runner validates required keys before execution and scopes outputs to `nodeId.key`. |
| `Dictionary<string, object?>` constructor | The runner uses this to instantiate nodes dynamically — no switch-case needed. |
| Call `NodeParameters.*` helpers | They handle `JsonElement` (from the web layer) and boxed primitives (from code-first) transparently. |

### NodeParameters helpers

```csharp
NodeParameters.GetString(p, "key", "default")
NodeParameters.GetBool(p, "key", false)
NodeParameters.GetInt(p, "key", 0)
NodeParameters.GetDouble(p, "key", 0.0)
NodeParameters.GetStringDict(p, "key")     // → Dictionary<string, string>?
NodeParameters.GetStringList(p, "key")    // → List<string>?
```

### WorkflowData API inside RunAsync

```csharp
// Reading
var text   = input.GetRequiredString("key");   // throws if missing
var text2  = input.GetString("key");            // returns null if missing
var num    = input.Get<int>("key");
var exists = input.Has("key");

// Writing (always clone first)
return Task.FromResult(
    input.Clone()
         .Set("output_key", value)
         .Set("count", 42));
```

---

## Step 2 — Add the UI schema inside the node class

Add a `public static NodeParameterSchema Schema { get; }` property directly in your node class. `NodeSchemaProvider` discovers it automatically via reflection — no registration needed.

```csharp
/// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
public static NodeParameterSchema Schema { get; } = new()
{
    NodeType    = "MyCustomNode",
    Description = "One-line description shown in the palette tooltip",
    Parameters  =
    [
        new()
        {
            Name         = "myParam",          // must match the Dictionary constructor key
            Label        = "My Parameter",     // shown above the input field
            Type         = ParameterType.Text,
            Required     = true,
            DefaultValue = "default value",    // pre-filled when the node is dropped
            Placeholder  = "e.g. some hint",
            Description  = "Shown below the field as help text",
        },
        // add more parameters as needed
    ]
};
```

Place this block immediately before the private fields — it reads like a spec for the node.

### Available parameter types

| `ParameterType` | Renders as | Extra properties |
|---|---|---|
| `Text` | Single-line input | `Placeholder` |
| `TextArea` | Multi-line textarea | `Placeholder` |
| `Number` | Numeric input | `MinValue`, `MaxValue`, `DefaultValue` |
| `Boolean` | Checkbox | `DefaultValue` (bool) |
| `Select` | Dropdown | `Options: [{ Value, Label }]` |
| `Json` | Monospace textarea | `Placeholder` |
| `Color` | Color picker | — |

> **DataIn / DataOut** are populated automatically from your `DataIn`/`DataOut` properties by `NodeDataMetadataProvider` at seed time. Do not declare them inside `Schema`.

---

## Step 3 — Register in the node palette

Open `source/web/Services/NodeTypeSeeder.cs` and add one line to `GetNodeMetadata()`:

```csharp
("MyCustomNode", "My Custom Node", "Data", "Short description for the palette tooltip", "#7ED321", "bi-box"),
//  ^ class name    ^ palette label   ^ cat   ^ tooltip text                              ^ colour   ^ Bootstrap Icon
```

Pick a colour and icon that matches your category:

| Category | Colour | Example icon |
|---|---|---|
| AI | `#4A90E2` | `bi-chat-left-dots` |
| Control | `#F5A623` | `bi-signpost-split` |
| Data | `#7ED321` | `bi-funnel` |
| IO | `#BD10E0` | `bi-globe` |

Browse Bootstrap Icons at [icons.getbootstrap.com](https://icons.getbootstrap.com).

On next startup the seeder upserts all node types — your node will appear in the palette immediately.

---

## Step 4 — Routing ports (only if non-standard)

The default is one `input` handle on the left and one `output` handle on the right. Skip this step if that is all you need.

If your node has multiple outputs (e.g. `success`/`error`, or named cases), add an entry to `NODE_ROUTING_PORTS` in `source/web/designer-react/src/nodeConfig.js`:

```js
MyCustomNode: {
  inputs:  [{ id: 'input',   label: 'Input'   }],
  outputs: [{ id: 'success', label: 'Success' },
            { id: 'error',   label: 'Error'   }],
},
```

Port `id` values must match what your node writes to `WorkflowData` for routing:

```csharp
// In RunAsync — write the activated port name so the runner knows which edge to follow
return Task.FromResult(
    input.Clone()
         .Set("my_route", isError ? "error" : "success"));
```

Then override `GetActivatedPort` in `WorkflowDefinitionRunner` for the new type (same pattern as `BranchNode` / `ErrorRouteNode`).

---

## Complete example — `SentimentNode`

A node that classifies text sentiment and routes to `positive` or `negative`.

### `source/core/Nodes/AI/SentimentNode.cs`

```csharp
using Twf.Flow.Core;
using Twf.Flow.Nodes;

namespace Twf.Flow.Nodes.AI;

public sealed class SentimentNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "AI";
    public override string Description => "Classifies text as positive or negative";
    public override string IdPrefix => "sentiment";

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new("text", typeof(string), Required: true, "Text to classify"),
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new("sentiment",       typeof(string), Description: "\"positive\" or \"negative\""),
        new("sentiment_score", typeof(double), Description: "Confidence 0–1"),
    ];

    // ── UI schema (discovered automatically by NodeSchemaProvider) ────────────

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "SentimentNode",
        Description = "Classify text as positive or negative",
        Parameters  =
        [
            new() { Name = "positiveWords", Label = "Positive Keywords (JSON array)",
                Type = ParameterType.Json, Required = false,
                DefaultValue = "[\"good\",\"great\",\"excellent\",\"love\",\"happy\"]",
                Placeholder  = "[\"good\", \"great\", ...]",
                Description  = "Words that count toward a positive score" },
        ]
    };

    // ── Constructors ──────────────────────────────────────────────────────────

    private readonly string[] _positiveWords;

    public SentimentNode(string name, string[]? positiveWords = null)
    {
        Name           = name;
        _positiveWords = positiveWords ["good", "great", "excellent", "love", "happy"];
    }

    // Dictionary constructor — runner uses this for dynamic instantiation
    public SentimentNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") "Sentiment",
            NodeParameters.GetStringList(parameters, "positiveWords")?.ToArray())
    { }

    // ── Execution ─────────────────────────────────────────────────────────────

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var text    = input.GetRequiredString("text").ToLower();
        var matches = _positiveWords.Count(w => text.Contains(w));
        var score   = Math.Min(1.0, matches / 3.0);
        var label   = score >= 0.5 ? "positive" : "negative";

        nodeCtx.Log($"Sentiment: {label} (score={score:F2})");

        return Task.FromResult(
            input.Clone()
                 .Set("sentiment",       label)
                 .Set("sentiment_score", score));
    }
}
```

### `NodeTypeSeeder.cs` entry

```csharp
("SentimentNode", "Sentiment", "AI", "Classify text as positive or negative", "#4A90E2", "bi-emoji-smile"),
```

That's everything. The schema is inside the node file. `NodeSchemaProvider` discovers it automatically. No changes to the runner, no frontend parameter maps.

---

## Checklist

- [ ] `source/core/Nodes/<Category>/MyCustomNode.cs` — implements `BaseNode`, has `Dictionary<string, object?>` constructor
- [ ] `source/web/Services/NodeSchemaProvider.cs` — schema entry added to `GetAllSchemas()`
- [ ] `source/web/Services/NodeTypeSeeder.cs` — one line added to `GetNodeMetadata()`
- [ ] `source/web/designer-react/src/nodeConfig.js` — `NODE_ROUTING_PORTS` entry added **only** if non-standard ports
- [ ] Build passes: `dotnet build source/core/ && dotnet build source/web/`
