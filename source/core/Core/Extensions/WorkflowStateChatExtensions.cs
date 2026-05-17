using twf_ai_framework.Core.Models;

namespace Twf.Flow.Core.Extensions;

/// <summary>
/// Extension methods for chat-specific workflow state operations.
/// Demonstrates how domain-specific logic can extend the core framework
/// without polluting the base <see cref="IWorkflowState"/> interface.
/// </summary>
public static class WorkflowStateChatExtensions
{
    private const string ChatHistoryKey = "__chat_history__";

    /// <summary>
    /// Appends a message to the chat history.
/// </summary>
 /// <param name="state">The workflow state.</param>
    /// <param name="message">The message to append.</param>
    /// <example>
  /// <code>
    /// context.State.AppendMessage(ChatMessage.User("Hello"));
    /// context.State.AppendMessage(ChatMessage.Assistant("Hi there!"));
    /// </code>
    /// </example>
    public static void AppendMessage(this IWorkflowState state, ChatMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

      var history = state.Get<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();
        history.Add(message);
        state.Set(ChatHistoryKey, history);
    }

    /// <summary>
    /// Retrieves the complete chat history.
    /// </summary>
    /// <param name="state">The workflow state.</param>
    /// <returns>A list of chat messages in chronological order.</returns>
    /// <example>
    /// <code>
    /// var messages = context.State.GetChatHistory();
    /// foreach (var msg in messages)
    /// {
    ///     Console.WriteLine($"{msg.Role}: {msg.Content}");
    /// }
    /// </code>
    /// </example>
    public static List<ChatMessage> GetChatHistory(this IWorkflowState state)
 {
  return state.Get<List<ChatMessage>>(ChatHistoryKey) ?? new List<ChatMessage>();
 }

    /// <summary>
    /// Clears all chat history from the workflow state.
    /// </summary>
    /// <param name="state">The workflow state.</param>
    /// <example>
    /// <code>
    /// context.State.ClearChatHistory(); // Start fresh conversation
    /// </code>
    /// </example>
    public static void ClearChatHistory(this IWorkflowState state)
    {
        state.Remove(ChatHistoryKey);
    }

    /// <summary>
    /// Gets the number of messages in the chat history.
    /// </summary>
    /// <param name="state">The workflow state.</param>
    /// <returns>The count of messages.</returns>
    public static int GetChatHistoryCount(this IWorkflowState state)
    {
        return state.Get<List<ChatMessage>>(ChatHistoryKey)?.Count ?? 0;
    }

  /// <summary>
    /// Gets the last N messages from the chat history.
    /// </summary>
    /// <param name="state">The workflow state.</param>
 /// <param name="count">The number of recent messages to retrieve.</param>
    /// <returns>The most recent messages.</returns>
    /// <example>
    /// <code>
    /// // Get last 5 messages for context window
    /// var recentMessages = context.State.GetRecentChatHistory(5);
    /// </code>
    /// </example>
    public static List<ChatMessage> GetRecentChatHistory(this IWorkflowState state, int count)
    {
        if (count <= 0)
     throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

        var history = state.GetChatHistory();
        return history.Skip(Math.Max(0, history.Count - count)).ToList();
    }
}
