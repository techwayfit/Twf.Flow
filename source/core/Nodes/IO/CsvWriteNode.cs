using System.Collections;
using System.Text;
using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.IO;

/// <summary>
/// Serializes a list of row objects from WorkflowData to a CSV string.
/// Each element in the list should be a dictionary or object with named properties.
/// Handles quoting of fields that contain commas, quotes, or newlines (RFC 4180).
///
/// Reads from WorkflowData:
///   - <see cref="_dataKey"/>: List of dicts / objects to serialize
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: CSV string
///   - csv_row_count: number of data rows written
/// </summary>
public sealed class CsvWriteNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "IO";
    public override string Description => $"Serializes '{_dataKey}' → CSV in '{_outputKey}'";

    // WorkflowData keys
    public const string DefaultDataKey   = "csv_rows";
    public const string DefaultOutputKey = "csv_output";
    public const string OutputRowCount   = "csv_row_count";

    private readonly string _dataKey;
    private readonly string _outputKey;
    private readonly bool   _includeHeader;
    private readonly char   _delimiter;

    public CsvWriteNode(string name, string dataKey, string outputKey = DefaultOutputKey, bool includeHeader = true, char delimiter = ',')
    {
        Name           = name;
        _dataKey       = dataKey;
        _outputKey     = outputKey;
        _includeHeader = includeHeader;
        _delimiter     = delimiter;
    }

    public CsvWriteNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "CSV Write",
            NodeParameters.GetString(parameters, "dataKey")   ?? DefaultDataKey,
            NodeParameters.GetString(parameters, "outputKey") ?? DefaultOutputKey,
            NodeParameters.GetBool(parameters, "includeHeader", true),
            (NodeParameters.GetString(parameters, "delimiter") ?? ",").FirstOrDefault(','))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var rows  = ExtractRows(input, _dataKey);
        var sb    = new StringBuilder();
        var count = 0;

        if (rows.Count > 0)
        {
            var columns = rows[0].Keys.ToList();

            if (_includeHeader)
                sb.AppendLine(BuildRow(columns.Select(c => c), _delimiter));

            foreach (var row in rows)
            {
                sb.AppendLine(BuildRow(columns.Select(c => row.TryGetValue(c, out var v) ? v : string.Empty), _delimiter));
                count++;
            }
        }

        // Trim trailing newline
        var csv = sb.ToString().TrimEnd('\r', '\n');

        var output = input.Clone()
            .Set(_outputKey,    csv)
            .Set(OutputRowCount, count);

        nodeCtx.Log($"Wrote {count} row(s) to CSV ({csv.Length} chars)");
        nodeCtx.SetMetadata("row_count", count);
        nodeCtx.SetMetadata("csv_length", csv.Length);

        return Task.FromResult(output);
    }

    private static List<Dictionary<string, string>> ExtractRows(WorkflowData data, string key)
    {
        var raw = data.Get<object>(key);
        if (raw is null) return [];

        IEnumerable<object?> items = raw switch
        {
            IEnumerable<object?> e => e,
            JsonElement je when je.ValueKind == JsonValueKind.Array
                => je.EnumerateArray().Select(e => (object?)e),
            _   => [raw],
        };

        var result = new List<Dictionary<string, string>>();
        foreach (var item in items)
        {
            var dict = ToStringDict(item);
            if (dict.Count > 0) result.Add(dict);
        }
        return result;
    }

    private static Dictionary<string, string> ToStringDict(object? item)
    {
        if (item is Dictionary<string, string> sdict)   return sdict;
        if (item is Dictionary<string, object?> odict)
            return odict.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);

        if (item is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return je.EnumerateObject()
                     .ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.String
                         ? p.Value.GetString() ?? string.Empty
                         : p.Value.ToString());

        // POCO — use reflection
        if (item is not null)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in item.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(item)?.ToString() ?? string.Empty;
            return dict;
        }

        return [];
    }

    private static string BuildRow(IEnumerable<string> fields, char delimiter)
    {
        return string.Join(delimiter, fields.Select(f => QuoteField(f, delimiter)));
    }

    private static string QuoteField(string value, char delimiter)
    {
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return '"' + value.Replace("\"", "\"\"") + '"';
        return value;
    }
}
