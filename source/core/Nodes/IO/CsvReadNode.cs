using Twf.Flow.Core;

namespace Twf.Flow.Nodes.IO;

/// <summary>
/// Parses a CSV string from WorkflowData into a list of row dictionaries.
/// Each row is represented as <c>Dictionary&lt;string, string&gt;</c> keyed by column name.
/// Handles quoted fields, embedded commas, and escaped quotes (RFC 4180).
///
/// Reads from WorkflowData:
///   - <see cref="_csvKey"/>: string containing CSV text
///
/// Writes to WorkflowData:
///   - <see cref="_outputKey"/>: List of Dictionary&lt;string,string&gt; (one per row)
///   - csv_row_count:  number of data rows parsed
///   - csv_columns:    list of column names (from header row)
/// </summary>
public sealed class CsvReadNode : BaseNode
{
    public override string Name     { get; }
    public override string Category => "IO";
    public override string Description => $"Parses CSV from '{_csvKey}' → '{_outputKey}'";
    public override string IdPrefix => "csvread";

    public override IReadOnlyList<NodeData> DataIn =>
    [
        new(_csvKey, typeof(string), Required: true, "String containing CSV text"),
    ];

    public override IReadOnlyList<NodeData> DataOut =>
    [
        new(_outputKey,    typeof(List<Dictionary<string, string>>), Description: "Parsed rows as list of dicts"),
        new(OutputRowCount, typeof(int),                              Description: "Number of data rows"),
        new(OutputColumns,  typeof(List<string>),                     Description: "Column names"),
    ];

    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "CsvReadNode",
        Description = "Parse CSV text into a list of row dictionaries",
        Parameters  =
        [
            new() { Name = "csvKey",    Label = "CSV Source Key", Type = ParameterType.Text, Required = true, Placeholder = "csv_text",
                Description = "WorkflowData key that holds the CSV string" },
            new() { Name = "outputKey", Label = "Output Key",     Type = ParameterType.Text, Required = false, DefaultValue = "csv_rows" },
            new() { Name = "hasHeader", Label = "First Row is Header", Type = ParameterType.Boolean, Required = false, DefaultValue = true,
                Description = "When true, the first row is treated as column names" },
            new() { Name = "delimiter", Label = "Delimiter",      Type = ParameterType.Text, Required = false, DefaultValue = ",",
                Placeholder = "," },
        ]
    };

    // WorkflowData keys
    public const string DefaultCsvKey    = "csv_text";
    public const string DefaultOutputKey = "csv_rows";
    public const string OutputRowCount   = "csv_row_count";
    public const string OutputColumns    = "csv_columns";

    private readonly string _csvKey;
    private readonly string _outputKey;
    private readonly bool   _hasHeader;
    private readonly char   _delimiter;

    public CsvReadNode(string name, string csvKey, string outputKey = DefaultOutputKey, bool hasHeader = true, char delimiter = ',')
    {
        Name       = name;
        _csvKey    = csvKey;
        _outputKey = outputKey;
        _hasHeader = hasHeader;
        _delimiter = delimiter;
    }

    public CsvReadNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name")      ?? "CSV Read",
            NodeParameters.GetString(parameters, "csvKey")    ?? DefaultCsvKey,
            NodeParameters.GetString(parameters, "outputKey") ?? DefaultOutputKey,
            NodeParameters.GetBool(parameters, "hasHeader", true),
            (NodeParameters.GetString(parameters, "delimiter") ?? ",").FirstOrDefault(','))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var csvText = input.GetString(_csvKey) ?? string.Empty;
        var allRows = ParseCsv(csvText, _delimiter);

        if (allRows.Count == 0)
        {
            nodeCtx.Log("⚠️ CSV text is empty");
            var empty = input.Clone()
                .Set(_outputKey,    new List<Dictionary<string, string>>())
                .Set(OutputRowCount, 0)
                .Set(OutputColumns,  new List<string>());
            return Task.FromResult(empty);
        }

        List<string> columns;
        List<Dictionary<string, string>> rows;

        if (_hasHeader)
        {
            columns = allRows[0];
            rows    = allRows.Skip(1)
                             .Select(row => ZipToDictionary(columns, row))
                             .ToList();
        }
        else
        {
            columns = Enumerable.Range(0, allRows[0].Count)
                                .Select(i => $"col{i}")
                                .ToList();
            rows = allRows.Select(row => ZipToDictionary(columns, row)).ToList();
        }

        var output = input.Clone()
            .Set(_outputKey,    rows)
            .Set(OutputRowCount, rows.Count)
            .Set(OutputColumns,  columns);

        nodeCtx.Log($"Parsed {rows.Count} row(s), {columns.Count} column(s)");
        nodeCtx.SetMetadata("row_count",    rows.Count);
        nodeCtx.SetMetadata("column_count", columns.Count);

        return Task.FromResult(output);
    }

    // ── RFC 4180 CSV parser ────────────────────────────────────────────────────

    private static List<List<string>> ParseCsv(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text)) return rows;

        var lines = SplitIntoLines(text);

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            rows.Add(ParseRow(line, delimiter));
        }

        return rows;
    }

    private static List<string> SplitIntoLines(string text)
    {
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                current.Append(ch);
            }
            else if (!inQuotes && (ch == '\n' || ch == '\r'))
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++; // skip \r\n pair
                lines.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            lines.Add(current.ToString());

        return lines;
    }

    private static List<string> ParseRow(string line, char delimiter)
    {
        var fields   = new List<string>();
        var current  = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"'); // escaped quote
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == delimiter)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static Dictionary<string, string> ZipToDictionary(List<string> keys, List<string> values)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < keys.Count; i++)
            dict[keys[i]] = i < values.Count ? values[i] : string.Empty;
        return dict;
    }
}
