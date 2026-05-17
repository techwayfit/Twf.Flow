namespace Twf.Flow.Core;

/// <summary>
/// Abstraction for workflow state management.
/// Provides a scoped key-value store for sharing data between nodes during execution.
/// </summary>
/// <remarks>
/// This interface separates state management concerns from infrastructure concerns
/// (logging, tracking, cancellation) in <see cref="WorkflowContext"/>.
/// </remarks>
public interface IWorkflowState
{
    /// <summary>
    /// Stores a value in the workflow state.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The key to store the value under.</param>
/// <param name="value">The value to store.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Retrieves a value from the workflow state.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The value if found, otherwise default(T).</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Checks if a key exists in the workflow state.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    bool Has(string key);

    /// <summary>
    /// Removes a key from the workflow state.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    void Remove(string key);

    /// <summary>
 /// Gets all state entries as a read-only dictionary.
    /// </summary>
    /// <returns>A snapshot of the current state.</returns>
    IReadOnlyDictionary<string, object?> GetAll();

    /// <summary>
  /// Clears all state entries.
  /// </summary>
    void Clear();
}
