using System.Text.Json;

namespace Twf.Flow.Core;

/// <summary>
/// The dynamic data packet that flows between nodes — similar to n8n's JSON item payload.
/// Each node reads from it, transforms it, and writes results back into it.
/// Supports type-safe access, cloning, merging, and history tracking.
/// </summary>
public class WorkflowData
{
    private readonly Dictionary<string, object?> _store;
    private readonly List<string> _writeHistory = new();

    public WorkflowData()
    {
        _store = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private WorkflowData(Dictionary<string, object?> store)
    {
        _store = new Dictionary<string, object?>(store, StringComparer.OrdinalIgnoreCase);
    }

    // ─── Read ────────────────────────────────────────────────────────────────

    /// <summary>Get a typed value by key. Returns default(T) if not found.</summary>
    public T? Get<T>(string key)
    {
        if (!_store.TryGetValue(key, out var raw) || raw is null)
            return default;

        if (raw is T typed)
            return typed;

        // Handle JSON deserialization if the value was stored as JsonElement
        if (raw is JsonElement je)
            return je.Deserialize<T>();

        try { return (T)Convert.ChangeType(raw, typeof(T)); }
        catch { return default; }
    }

    /// <summary>Get a value or throw if not found.</summary>
    public T GetRequired<T>(string key)
    {
        var value = Get<T>(key);
        if (value is null)
            throw new KeyNotFoundException(
                $"Required key '{key}' not found in WorkflowData. " +
                $"Available keys: {string.Join(", ", _store.Keys)}");
        return value;
    }

    /// <summary>Get string value shorthand.</summary>
    public string? GetString(string key) => Get<string>(key);

    /// <summary>Get string value or throw.</summary>
    public string GetRequiredString(string key) => GetRequired<string>(key);

    /// <summary>Check if a key exists and is non-null.</summary>
    public bool Has(string key) =>
        _store.TryGetValue(key, out var val) && val is not null;

    /// <summary>Try get with out param pattern.</summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = Get<T>(key);
        return value is not null;
    }

    /// <summary>Get all keys currently in this data bag.</summary>
    public IReadOnlyList<string> Keys => _store.Keys.ToList();

    // ─── Write ───────────────────────────────────────────────────────────────

    /// <summary>Set a value by key. Records write history for debugging.</summary>
    public WorkflowData Set<T>(string key, T value)
    {
        _store[key] = value;
        _writeHistory.Add(key);
        return this; // supports fluent chaining: data.Set(...).Set(...)
    }

    /// <summary>Set multiple values from a dictionary.</summary>
    public WorkflowData SetMany(IDictionary<string, object?> values)
    {
        foreach (var (k, v) in values)
            Set(k, v);
        return this;
    }

    /// <summary>Remove a key from the data bag.</summary>
    public WorkflowData Remove(string key)
    {
        _store.Remove(key);
        return this;
    }

    // ─── Merge / Clone ───────────────────────────────────────────────────────

    /// <summary>
    /// Merge another WorkflowData into this one.
    /// The other's values overwrite this one's values for duplicate keys.
    /// </summary>
    public WorkflowData Merge(WorkflowData other)
    {
        foreach (var (k, v) in other._store)
            _store[k] = v;
        return this;
    }

    /// <summary>Return a deep copy of this WorkflowData.</summary>
    public WorkflowData Clone()
    {
        var clone = new WorkflowData(_store);
        return clone;
    }

    /// <summary>Create a new WorkflowData with a single initial key-value.</summary>
    public static WorkflowData From<T>(string key, T value) =>
        new WorkflowData().Set(key, value);

    /// <summary>Create a new WorkflowData from a dictionary.</summary>
    public static WorkflowData FromDictionary(IDictionary<string, object?> dict) =>
        new WorkflowData().SetMany(dict);

    // ─── Serialization ───────────────────────────────────────────────────────

    /// <summary>Serialize to JSON string for logging/debugging.</summary>
    public string ToJson(bool indented = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };
        return JsonSerializer.Serialize(_store, options);
    }

    /// <summary>Deserialize a JSON string into a new WorkflowData.</summary>
    public static WorkflowData FromJson(string json)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                   ?? new Dictionary<string, object?>();
        return FromDictionary(dict);
    }

    // ─── Diagnostics ─────────────────────────────────────────────────────────

    public IReadOnlyList<string> WriteHistory => _writeHistory.AsReadOnly();

    public override string ToString() =>
        $"WorkflowData[{_store.Count} keys: {string.Join(", ", _store.Keys)}]";
}
