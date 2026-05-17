using Twf.Flow.Tracking;
using Microsoft.Extensions.Logging;
using twf_ai_framework.Core.Models;
using Twf.Flow.Core.Extensions;

namespace Twf.Flow.Core;

/// <summary>
/// Execution context for a workflow run.
/// Provides infrastructure (logging, tracking, cancellation) and scoped state management.
/// </summary>
/// <remarks>
/// Refactored to follow Single Responsibility Principle:
/// - Infrastructure concerns (logging, tracking) remain here
/// - State management delegated to <see cref="IWorkflowState"/>
/// - Domain logic (chat history) moved to extension methods
/// - Service locator pattern removed (use proper DI instead)
/// </remarks>
public sealed class WorkflowContext
{
    // ─── Identity ────────────────────────────────────────────────────────────

    /// <summary>The name of the workflow (e.g., "CustomerSupportBot").</summary>
    public string WorkflowName { get; }

    /// <summary>Unique ID for this specific execution run.</summary>
    public string RunId { get; }

    /// <summary>UTC timestamp when this run was started.</summary>
    public DateTime StartedAt { get; }

    // ─── Infrastructure ──────────────────────────────────────────────────────

    /// <summary>Structured logger for workflow events.</summary>
    public ILogger Logger { get; }

    /// <summary>Execution tracker — records every node's start/end/status.</summary>
    public ExecutionTracker Tracker { get; }

    /// <summary>Cancellation token for the entire workflow run.</summary>
    public CancellationToken CancellationToken { get; }

    // ─── State Management ─────────────────────────────────────────────────────

    /// <summary>
    /// Workflow state for sharing data between nodes.
    /// Use extension methods (e.g., <see cref="WorkflowStateChatExtensions.AppendMessage"/>)
    /// for domain-specific operations.
    /// </summary>
    public IWorkflowState State { get; }

    // ─── Constructor ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new workflow context for a workflow run.
    /// </summary>
    /// <param name="workflowName">The name of the workflow.</param>
    /// <param name="logger">Logger for workflow events.</param>
    /// <param name="tracker">Optional execution tracker (creates default if null).</param>
    /// <param name="cancellationToken">Cancellation token for the workflow.</param>
    public WorkflowContext(
        string workflowName,
     ILogger logger,
        ExecutionTracker? tracker = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowName))
            throw new ArgumentException("Workflow name cannot be empty", nameof(workflowName));

        WorkflowName = workflowName;
        RunId = Guid.NewGuid().ToString("N")[..12].ToUpper();
        StartedAt = DateTime.UtcNow;
        Logger = new PrefixedLogger(logger, $"[{workflowName}][{RunId}]");
        Tracker = tracker ?? new ExecutionTracker();
        CancellationToken = cancellationToken;
        State = new WorkflowState();
    }

    // ─── Backward Compatibility Methods (Deprecated) ─────────────────────────
    // These delegate to State for backward compatibility with existing code.
    // New code should use context.State.Set() directly.

    /// <summary>
    /// Stores a value in the workflow state.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.Set(key, value)</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.Set() instead")]
    public void SetState<T>(string key, T value) => State.Set(key, value);

    /// <summary>
    /// Retrieves a value from the workflow state.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.Get&lt;T&gt;(key)</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.Get<T>() instead")]
    public T? GetState<T>(string key) => State.Get<T>(key);

    /// <summary>
    /// Checks if a key exists in the workflow state.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.Has(key)</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.Has() instead")]
    public bool HasState(string key) => State.Has(key);

    // ─── Chat History Methods (Backward Compatibility) ────────────────────────
    // These delegate to extension methods for backward compatibility.

    /// <summary>
    /// Appends a message to the chat history.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.AppendMessage(message)</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.AppendMessage() extension method instead")]
    public void AppendMessage(ChatMessage message) => State.AppendMessage(message);

    /// <summary>
    /// Gets the chat history.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.GetChatHistory()</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.GetChatHistory() extension method instead")]
    public List<ChatMessage> GetChatHistory() => State.GetChatHistory();

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deprecated: Use <c>context.State.ClearChatHistory()</c> instead.
    /// </remarks>
    [Obsolete("Use context.State.ClearChatHistory() extension method instead")]
    public void ClearChatHistory() => State.ClearChatHistory();

    // ─── Service Locator (Removed) ────────────────────────────────────────────
    // Service locator pattern removed - use proper dependency injection instead.
    // If you need services in nodes, inject them through constructors.

    public override string ToString() => $"WorkflowContext('{WorkflowName}', RunId={RunId})";
}
