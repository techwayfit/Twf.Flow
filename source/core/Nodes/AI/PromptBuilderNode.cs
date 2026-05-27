using Twf.Flow.Core;
using Twf.Flow.Nodes;
using System.Text.RegularExpressions;

namespace Twf.Flow.Nodes.AI;

/// <summary>
/// Builds dynamic prompts from templates with {{variable}} substitution.
/// 
/// Template example:
///   "You are a {{role}}. Answer the following question: {{question}}"
///
/// Reads from WorkflowData:
///   - Keys matching {{variable}} names in the template
///
/// Writes to WorkflowData:
/// - "prompt" : the fully substituted prompt string
///   - "system_prompt" : if a system template is also provided
/// </summary>
/// <example>
/// <code>
/// // Example 1: Simple template substitution
/// var promptNode = new PromptBuilderNode(
///     "BuildGreeting",
///     promptTemplate: "Hello {{name}}, welcome to {{company}}!"
/// );
/// 
/// var input = new WorkflowData()
///     .Set("name", "Alice")
///     .Set("company", "TechCorp");
///
/// var result = await promptNode.ExecuteAsync(input, context);
/// var prompt = result.Data.GetString("prompt");
/// // Output: "Hello Alice, welcome to TechCorp!"
/// 
/// // Example 2: System + User prompt for LLM
/// var chatNode = new PromptBuilderNode(
///     "ChatPrompt",
///     promptTemplate: "User question: {{question}}",
///     systemTemplate: "You are a {{role}}. {{instructions}}"
/// );
/// 
/// var chatInput = new WorkflowData()
///     .Set("question", "What is .NET?")
///     .Set("role", "senior software engineer")
///     .Set("instructions", "Answer concisely with code examples.");
/// 
/// var result = await chatNode.ExecuteAsync(chatInput, context);
/// var userPrompt = result.Data.GetString("prompt");
/// var systemPrompt = result.Data.GetString("system_prompt");
/// // prompt: "User question: What is .NET?"
/// // system_prompt: "You are a senior software engineer. Answer concisely with code examples."
/// 
/// // Example 3: Static variables (constants)
/// var templateNode = new PromptBuilderNode(
///     "StaticTemplate",
///  promptTemplate: "Translate '{{text}}' to {{language}}",
///   staticVariables: new Dictionary&lt;string, object?&gt;
///     {
///         ["language"] = "French"  // Always use French
///     }
/// );
/// 
/// var input = new WorkflowData().Set("text", "Hello world");
/// var result = await templateNode.ExecuteAsync(input, context);
/// // prompt: "Translate 'Hello world' to French"
/// 
/// // Example 4: Complex multi-variable template
/// var summaryNode = new PromptBuilderNode(
///  "SummarizeArticle",
///     promptTemplate: """
///         Summarize this article:
///         Title: {{title}}
/// Author: {{author}}
///         Content: {{content}}
///         
///       Target audience: {{audience}}
///    Max length: {{max_words}} words
///      """,
///     systemTemplate: "You are an expert content editor."
/// );
/// 
/// var article = new WorkflowData()
///     .Set("title", "AI in Healthcare")
///     .Set("author", "Dr. Smith")
///   .Set("content", "Long article text here...")
///     .Set("audience", "medical professionals")
///     .Set("max_words", "150");
/// 
/// // Example 5: Use in LLM workflow
/// var workflow = WorkflowBuilder.Create("ChatBot")
///   .AddNode(new PromptBuilderNode(
///      "BuildPrompt",
///         promptTemplate: "{{user_message}}",
///     systemTemplate: "You are a {{persona}}. {{tone}}"
/// ))
///     .AddNode(new LlmNode("GenerateResponse", llmConfig))
///     .AddNode(new OutputParserNode("ParseJSON"));
/// 
/// var chat = new WorkflowData()
///     .Set("user_message", "Explain quantum computing")
///     .Set("persona", "physics professor")
///     .Set("tone", "Use simple analogies for beginners.");
///     
/// var result = await workflow.RunAsync(chat);
/// 
/// // Example 6: Missing variable handling
/// var node = new PromptBuilderNode("Test", "Hello {{missing_var}}!");
/// var input = new WorkflowData(); // no "missing_var" key
/// var result = await node.ExecuteAsync(input, context);
/// var prompt = result.Data.GetString("prompt");
/// // Output: "Hello {{MISSING:missing_var}}!" (shows what's missing)
/// 
/// // Example 7: Load templates from files
/// var fileNode = PromptBuilderNode.FromFile(
///     "LoadedPrompt",
///     templatePath: "prompts/system.txt",
///     systemPath: "prompts/user.txt"
/// );
/// 
/// // Example 8: Factory methods
/// var simplePrompt = PromptBuilderNode.Simple("Quick", "Summarize: {{text}}");
/// var withSystem = PromptBuilderNode.WithSystem(
///     "Detailed",
///     prompt: "Translate: {{text}}",
///     system: "You are a professional translator."
/// );
/// </code>
/// </example>
public sealed class PromptBuilderNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "AI";
    public override string Description => Schema.Description;

    /// <inheritdoc/>
    public override string IdPrefix => "prompt";

    // WorkflowData keys
    public const string OutputPrompt       = "prompt";
    public const string OutputSystemPrompt = "system_prompt";

    /// <inheritdoc/>
    // Input ports are the {{variable}} placeholders found in the templates at construction time.
    public override IReadOnlyList<NodeData> DataIn => ExtractTemplatePorts();

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(OutputPrompt,       typeof(string), Description: "Rendered prompt text"),
        new(OutputSystemPrompt, typeof(string), Required: false, Description: "Rendered system instruction")
    ];

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "PromptBuilderNode",
        Description = "Build a dynamic prompt from a template with {{variable}} slots",
        Parameters  =
        [
            new() { Name = "promptTemplate", Label = "Prompt Template", Type = ParameterType.TextArea, Required = true,
                Placeholder = "Use {{variable}} syntax, e.g. 'Summarize: {{content}}'" },
            new() { Name = "systemTemplate", Label = "System Template", Type = ParameterType.TextArea, Required = false,
                Placeholder = "Optional system prompt template" },
        ]
    };

    private readonly string _promptTemplate;
    private readonly string? _systemTemplate;
    private readonly Dictionary<string, object?> _staticVariables;

    public PromptBuilderNode(
        string name,
        string promptTemplate,
        string? systemTemplate = null,
        Dictionary<string, object?>? staticVariables = null)
    {
        Name = name;
        _promptTemplate = promptTemplate;
        _systemTemplate = systemTemplate;
        _staticVariables = staticVariables ?? new();
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public PromptBuilderNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Prompt Builder",
            NodeParameters.GetString(parameters, "promptTemplate") ?? "",
            NodeParameters.GetString(parameters, "systemTemplate"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var prompt = Render(_promptTemplate, input, nodeCtx);
        var output = input.Clone().Set(OutputPrompt, prompt);

        nodeCtx.Log($"Prompt length: {prompt.Length} chars");

        if (_systemTemplate is not null)
        {
            var system = Render(_systemTemplate, input, nodeCtx);
            output.Set(OutputSystemPrompt, system);
            nodeCtx.Log($"System prompt length: {system.Length} chars");
        }

        return Task.FromResult(output);
    }

    private string Render(string template, WorkflowData input, NodeExecutionContext nodeCtx)
    {
        return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
        {
            var key = match.Groups[1].Value;

            // Check static variables first
            if (_staticVariables.TryGetValue(key, out var staticVal))
                return staticVal?.ToString() ?? string.Empty;

            // Then check WorkflowData
            if (input.TryGet<object>(key, out var val) && val is not null)
                return val.ToString() ?? string.Empty;

            nodeCtx.Log($"⚠️  Template variable '{{{{key}}}}' not found in data");
            return $"{{{{MISSING:{key}}}}}";
        });
    }

    // ─── Port helpers ────────────────────────────────────────────────────────

    private IReadOnlyList<NodeData> ExtractTemplatePorts()
    {
        var keys = new HashSet<string>();
        foreach (Match m in Regex.Matches(_promptTemplate, @"\{\{(\w+)\}\}"))
            keys.Add(m.Groups[1].Value);
        if (_systemTemplate is not null)
            foreach (Match m in Regex.Matches(_systemTemplate, @"\{\{(\w+)\}\}"))
                keys.Add(m.Groups[1].Value);

        return keys
            .Select(k => new NodeData(k, typeof(string), Required: false,
                Description: $"Template variable {{{{{k}}}}}"))
            .ToList();
    }

    // ─── Convenience Factory Methods ──────────────────────────────────────────

    public static PromptBuilderNode Simple(string name, string template) =>
        new(name, template);

    public static PromptBuilderNode WithSystem(string name, string prompt, string system) =>
        new(name, prompt, system);

    public static PromptBuilderNode FromFile(string name, string templatePath, string? systemPath = null)
    {
        var prompt = File.ReadAllText(templatePath);
        var system = systemPath is not null ? File.ReadAllText(systemPath) : null;
        return new PromptBuilderNode(name, prompt, system);
    }
}
