using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes;

/// <summary>
/// Shared parameter-parsing helpers used by all node dictionary constructors.
/// Handles the JSON deserialization cases that arise when parameters arrive
/// from the web designer (JsonElement) or from code (boxed primitives).
/// </summary>
public static class NodeParameters
{
    /// <summary>
    /// Retrieves a string value from the parameter dictionary.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <param name="def">The default value to return if the key is not found or the value is null.</param>
    /// <returns>The string value associated with the key, or the default value if not found.</returns>
    public static string? GetString(IReadOnlyDictionary<string, object?> p, string key, string? def = null)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return def;
        if (raw is JsonElement je) return je.GetString() ?? def;
        return raw.ToString() ?? def;
    }
    /// <summary>
    /// Retrieves a boolean value from the parameter dictionary, handling various input formats.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <param name="def">The default value to return if the key is not found or the value is null.</param>
    /// <returns>The boolean value associated with the key, or the default value if not found.</returns>
    public static bool GetBool(IReadOnlyDictionary<string, object?> p, string key, bool def = false)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return def;
        if (raw is bool b) return b;
        if (raw is JsonElement je)  
        {
            if (je.ValueKind == JsonValueKind.True)  return true;
            if (je.ValueKind == JsonValueKind.False) return false;
        }
        return bool.TryParse(raw.ToString(), out var parsed) ? parsed : def;
    }
    /// <summary>
    /// Retrieves an integer value from the parameter dictionary, handling various input formats.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <param name="def">The default value to return if the key is not found or the value is null.</param>
    /// <returns>The integer value associated with the key, or the default value if not found.</returns>
    public static int GetInt(IReadOnlyDictionary<string, object?> p, string key, int def = 0)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return def;
        if (raw is JsonElement je && je.TryGetInt32(out var v)) return v;
        if (raw is int i) return i;
        return int.TryParse(raw.ToString(), out var parsed) ? parsed : def;
    }

    /// <summary>
    /// Retrieves a double value from the parameter dictionary, handling various input formats.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <param name="def">The default value to return if the key is not found or the value is null.</param>
    /// <returns>The double value associated with the key, or the default value if not found.</returns>
    public static double GetDouble(IReadOnlyDictionary<string, object?> p, string key, double def = 0)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return def;
        if (raw is JsonElement je && je.TryGetDouble(out var v)) return v;
        if (raw is double d) return d;
        if (raw is float f)  return f;
        return double.TryParse(raw.ToString(), out var parsed) ? parsed : def;
    }
    /// <summary>
    /// Retrieves a dictionary of strings from the parameter dictionary, handling various input formats.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <returns>The dictionary of strings associated with the key, or null if not found.</returns>
    public static Dictionary<string, string>? GetStringDict(IReadOnlyDictionary<string, object?> p, string key)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return null;

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return je.EnumerateObject()
                     .Where(prop => prop.Value.ValueKind == JsonValueKind.String)
                     .ToDictionary(prop => prop.Name, prop => prop.Value.GetString()!);

        if (raw is Dictionary<string, string> dict) return dict;
        if (raw is Dictionary<string, object?> objDict)
            return objDict.Where(kv => kv.Value is not null)
                          .ToDictionary(kv => kv.Key, kv => kv.Value!.ToString()!);

        return null;
    }

    /// <summary>
    /// Retrieves a list of strings from the parameter dictionary, handling various input formats.
    /// </summary>
    /// <param name="p">The parameter dictionary to search.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <returns>The list of strings associated with the key, or null if not found.</returns>

    public static List<string>? GetStringList(IReadOnlyDictionary<string, object?> p, string key)
    {
        if (!p.TryGetValue(key, out var raw) || raw is null) return null;

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray()
                     .Where(e => e.ValueKind == JsonValueKind.String)
                     .Select(e => e.GetString()!)
                     .ToList();

        if (raw is List<string> list)         return list;
        if (raw is string[] arr)              return arr.ToList();
        if (raw is IEnumerable<string> ienum) return ienum.ToList();

        // Value is a JSON array serialised as a plain string — e.g. "[\"__item__\"]"
        if (raw is string s && s.TrimStart().StartsWith('['))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(s);
                if (parsed is not null) return parsed;
            }
            catch { /* fall through */ }
        }

        return null;
    }

    /// <summary>
    /// Resolves a value from <paramref name="data"/> by key, supporting dotted property paths.
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><c>"search_results_count"</c> — flat key lookup</item>
    ///   <item><c>"__item__.title"</c> — gets <c>__item__</c>, then accesses <c>Title</c> by reflection</item>
    ///   <item><c>"node001.result.value"</c> — nested traversal</item>
    /// </list>
    /// </para>
    /// Supports C# records/POCOs (reflection), <see cref="JsonElement"/>, and
    /// <see cref="IDictionary{TKey,TValue}"/> at each level.
    /// </summary>
    public static object? GetNestedValue(IReadOnlyDictionary<string, object?> data, string key)
    {
        // Fast path — direct hit (also handles scoped keys like "node001.search_results")
        if (data.TryGetValue(key, out var flat)) return flat;

        // Dotted path: split on first dot, recurse into the object
        var dot = key.IndexOf('.');
        if (dot <= 0) return null;

        var rootKey = key[..dot];
        if (!data.TryGetValue(rootKey, out var root) || root is null) return null;

        return TraversePropertyPath(root, key[(dot + 1)..]);
    }
    /// <summary>
    /// Extension method for WorkflowData to retrieve a nested value by key, supporting dotted property paths.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static object? NestedValue(this WorkflowData input,string path)
    {
        var val = NodeParameters.GetNestedValue(
                input.Keys.ToDictionary(k => k, input.Get<object>), path);
        return val;
    }

    /// <summary>
    /// Traverses a dotted property path on <paramref name="obj"/>, returning the terminal
    /// value as an <see cref="object"/> or <c>null</c> if any segment is missing.
    /// </summary>
    public static object? TraversePropertyPath(object obj, string path)
    {
        var dot      = path.IndexOf('.');
        var segment  = dot > 0 ? path[..dot] : path;
        var rest     = dot > 0 ? path[(dot + 1)..] : null;

        object? next = null;

        if (obj is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in je.EnumerateObject())
            {
                if (!string.Equals(p.Name, segment, StringComparison.OrdinalIgnoreCase)) continue;
                next = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : (object)p.Value;
                break;
            }
        }
        else if (obj is IDictionary<string, object?> dict)
        {
            var hit = dict.FirstOrDefault(kv =>
                string.Equals(kv.Key, segment, StringComparison.OrdinalIgnoreCase));
            next = hit.Value;
        }
        else
        {
            var prop = obj.GetType().GetProperty(segment,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            next = prop?.GetValue(obj);
        }

        if (next is null) return null;
        if (rest is null) return next;
        return TraversePropertyPath(next, rest);
    }
}
