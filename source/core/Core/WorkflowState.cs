namespace Twf.Flow.Core;

/// <summary>
/// Default implementation of <see cref="IWorkflowState"/> using an in-memory dictionary.
/// Thread-safe for concurrent access during workflow execution.
/// </summary>
internal sealed class WorkflowState : IWorkflowState
{
    private readonly Dictionary<string, object?> _store = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public void Set<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("State key cannot be empty", nameof(key));

        lock (_lock)
        {
            _store[key] = value;
        }
    }

    /// <inheritdoc/>
    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        lock (_lock)
        {
            return _store.TryGetValue(key, out var value) ? (T?)value : default;
        }
    }

    /// <inheritdoc/>
    public bool Has(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        lock (_lock)
        {
            return _store.ContainsKey(key);
        }
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        lock (_lock)
        {
            _store.Remove(key);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> GetAll()
    {
        lock (_lock)
        {
            return new Dictionary<string, object?>(_store);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            _store.Clear();
        }
    }

    public override string ToString() => $"WorkflowState(Count={_store.Count})";
}
