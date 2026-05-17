using System.Collections;
using System.Reflection;
using System.Text.Json;
using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Data;

/// <summary>
/// Maps values from source keys/paths to target keys in WorkflowData.
/// Useful for explicitly wiring one node's output shape into another node's input shape.
///
/// Source path examples:
///   - "llm_response"
///   - "http_response.data.id"
///   - "{{http_response.data.id}}" (template braces are allowed)
///   - "parsed_output.items.0.name" (array index supported for JSON arrays)
/// </summary>
/// <example>
/// <code>
/// // Example 1: Simple key renaming
/// var mapper = new DataMapperNode("RenameFields", new Dictionary&lt;string, string&gt;
/// {
///     ["input_name"] = "user_name",      // Rename input_name → user_name
///     ["input_email"] = "email_address"  // Rename input_email → email_address
/// });
/// 
/// var input = new WorkflowData()
///     .Set("input_name", "John Doe")
///     .Set("input_email", "john@example.com");
///     
/// var result = await mapper.ExecuteAsync(input, context);
/// // Output: user_name="John Doe", email_address="john@example.com", input_name="John Doe", input_email="john@example.com"
/// 
/// // Example 2: Extract nested data
/// var nestedMapper = new DataMapperNode("ExtractNested", new Dictionary&lt;string, string&gt;
/// {
///     ["customer_id"] = "response.data.customer.id",
///     ["full_name"] = "response.data.customer.name",
///["subscription"] = "response.data.plan.type"
/// });
/// 
/// var apiResponse = new WorkflowData().Set("response", new
/// {
///     data = new
///     {
///         customer = new { id = "cust_123", name = "Jane Smith" },
///     plan = new { type = "premium" }
///     }
/// });
/// 
/// var result = await nestedMapper.ExecuteAsync(apiResponse, context);
/// // Output: customer_id="cust_123", full_name="Jane Smith", subscription="premium"
/// 
/// // Example 3: Default values for missing data
/// var safeMapper = new DataMapperNode(
/// "SafeMapping",
///     mappings: new Dictionary&lt;string, string&gt; { ["country"] = "address.country" },
///     defaultValues: new Dictionary&lt;string, object?&gt; { ["country"] = "US" }
/// );
/// 
/// var incompleteData = new WorkflowData().Set("address", new { city = "Seattle" });
/// var result = await safeMapper.ExecuteAsync(incompleteData, context);
/// // Output: country="US" (fallback used)
/// 
/// // Example 4: Strict mode — fail on missing data
/// var strictMapper = new DataMapperNode(
///     "StrictMapping",
///     mappings: new Dictionary&lt;string, string&gt; { ["required_field"] = "source.required" },
///     throwOnMissing: true
/// );
/// 
/// try
/// {
///     var result = await strictMapper.ExecuteAsync(emptyData, context);
/// }
/// catch (Exception ex)
/// {
///  // Throws exception if source.required is missing
/// }
/// 
/// // Example 5: Clean output — remove unmapped fields
/// var cleanMapper = new DataMapperNode(
///     "CleanMapper",
///   mappings: new Dictionary&lt;string, string&gt;
///     {
///         ["name"] = "user.name",
///         ["email"] = "user.email"
///     },
///     removeUnmapped: true  // Only include mapped fields in output
/// );
/// 
/// var messyInput = new WorkflowData()
///     .Set("user", new { name = "Bob", email = "bob@test.com" })
///     .Set("temp_value", "ignore_this")
///   .Set("debug_info", "also_ignore");
///     
/// var result = await cleanMapper.ExecuteAsync(messyInput, context);
/// // Output: ONLY name="Bob" and email="bob@test.com" (other keys removed)
/// 
/// // Example 6: Use in workflow (API response transformation)
/// var workflow = Workflow.Create("ProcessAPI")
///     .AddNode(new HttpNode("FetchData", httpConfig))
///     .AddNode(new DataMapperNode("TransformResponse", new Dictionary&lt;string, string&gt;
/// {
///         ["user_id"] = "response.body.data.id",
///         ["user_name"] = "response.body.data.attributes.name",
///  ["created_at"] = "response.body.data.attributes.createdAt"
///     }))
///     .AddNode(new DbQueryNode("SaveUser", dbConfig));
/// </code>
/// </example>
public sealed class DataMapperNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Data";
    public override string Description =>
        $"Maps {_mappings.Count} field(s) from source paths to target keys";

    /// <inheritdoc/>
    public override string IdPrefix => "mapper";

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataIn =>
        _mappings.Values
                 .Select(src => new NodeData(src.Split('.')[0], typeof(object), Required: false, "Source path root"))
                 .DistinctBy(p => p.Key)
                 .ToList<NodeData>();

    /// <inheritdoc/>
    public override IReadOnlyList<NodeData> DataOut =>
        _mappings.Keys
                 .Select(k => new NodeData(k, typeof(object), Description: "Mapped output key"))
                 .ToList<NodeData>();

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "DataMapperNode",
        Description = "Map source paths to target keys with dot-path support",
        Parameters  =
        [
            new() { Name = "mappings",       Label = "Mappings (JSON)",        Type = ParameterType.Json,    Required = true,
                Placeholder = "{\"prompt\": \"llm_response\", \"user_id\": \"http_response.data.id\"}",
                Description = "targetKey → sourcePath. Source supports dot-path notation." },
            new() { Name = "throwOnMissing", Label = "Throw on Missing Source",Type = ParameterType.Boolean, Required = false, DefaultValue = false },
            new() { Name = "removeUnmapped", Label = "Output Only Mapped Keys",Type = ParameterType.Boolean, Required = false, DefaultValue = false,
                Description = "If enabled, unmapped keys are dropped from output" },
        ]
    };

    private readonly IReadOnlyDictionary<string, string> _mappings;
    private readonly IReadOnlyDictionary<string, object?> _defaultValues;
    private readonly bool _throwOnMissing;
    private readonly bool _removeUnmapped;

    public DataMapperNode(
        string name,
        IDictionary<string, string> mappings,
        IDictionary<string, object?>? defaultValues = null,
        bool throwOnMissing = false,
        bool removeUnmapped = false)
    {
        Name = name;
        _mappings = new Dictionary<string, string>(mappings, StringComparer.OrdinalIgnoreCase);
        _defaultValues = defaultValues is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(defaultValues, StringComparer.OrdinalIgnoreCase);
        _throwOnMissing = throwOnMissing;
        _removeUnmapped = removeUnmapped;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public DataMapperNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Data Mapper",
            NodeParameters.GetStringDict(parameters, "mappings") ?? new Dictionary<string, string>(),
            null,
            NodeParameters.GetBool(parameters, "throwOnMissing"),
            NodeParameters.GetBool(parameters, "removeUnmapped"))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var output = _removeUnmapped ? new WorkflowData() : input.Clone();

        var mappedCount = 0;
        var missingCount = 0;
        var missingTargets = new List<string>();

        foreach (var (targetKey, sourcePath) in _mappings)
        {
            if (TryResolveSourcePath(input, sourcePath, out var value))
            {
                output.Set(targetKey, value);
                mappedCount++;
                continue;
            }

            if (_defaultValues.TryGetValue(targetKey, out var defaultValue))
            {
                output.Set(targetKey, defaultValue);
                mappedCount++;
                nodeCtx.Log($"Default applied for '{targetKey}'");
                continue;
            }

            missingCount++;
            missingTargets.Add(targetKey);
            nodeCtx.Log($"⚠️  Mapping missing: '{sourcePath}' → '{targetKey}'");
        }

        nodeCtx.SetMetadata("mapped_count", mappedCount);
        nodeCtx.SetMetadata("missing_count", missingCount);
        nodeCtx.SetMetadata("remove_unmapped", _removeUnmapped);

        if (_throwOnMissing && missingCount > 0)
        {
            throw new InvalidOperationException(
                $"[{Name}] Missing mapping source(s) for target key(s): {string.Join(", ", missingTargets)}");
        }

        return Task.FromResult(output);
    }

    public static DataMapperNode FromPairs(
        string name,
        params (string TargetKey, string SourcePath)[] mappings)
    {
        return new DataMapperNode(
            name,
            mappings.ToDictionary(m => m.TargetKey, m => m.SourcePath, StringComparer.OrdinalIgnoreCase));
    }

    private static bool TryResolveSourcePath(WorkflowData input, string sourcePath, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(sourcePath))
            return false;

        var normalized = NormalizeSourcePath(sourcePath);
        var segments = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return false;

        if (!input.Has(segments[0]))
            return false;

        object? current = input.Get<object>(segments[0]);

        for (var i = 1; i < segments.Length; i++)
        {
            if (!TryResolveChildValue(current, segments[i], out current))
                return false;
        }

        value = current;
        return true;
    }

    private static string NormalizeSourcePath(string sourcePath)
    {
        var trimmed = sourcePath.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal) &&
            trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return trimmed[2..^2].Trim();
        }
        return trimmed;
    }

    private static bool TryResolveChildValue(object? current, string segment, out object? child)
    {
        child = null;
        if (current is null)
            return false;

        switch (current)
        {
            case JsonElement jsonElement:
                return TryResolveJsonElement(jsonElement, segment, out child);

            case IDictionary<string, object?> dict:
                if (dict.TryGetValue(segment, out var directMatch))
                {
                    child = directMatch;
                    return true;
                }

                foreach (var (key, value) in dict)
                {
                    if (key.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        child = value;
                        return true;
                    }
                }

                return false;

            case IDictionary genericDict:
                foreach (DictionaryEntry entry in genericDict)
                {
                    if (entry.Key is string key &&
                        key.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        child = entry.Value;
                        return true;
                    }
                }
                return false;

            default:
                var property = current.GetType().GetProperty(
                    segment,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property is null)
                    return false;

                child = property.GetValue(current);
                return true;
        }
    }

    private static bool TryResolveJsonElement(JsonElement jsonElement, string segment, out object? value)
    {
        value = null;

        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            if (jsonElement.TryGetProperty(segment, out var exactMatch))
            {
                value = exactMatch;
                return true;
            }

            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        if (jsonElement.ValueKind == JsonValueKind.Array &&
            int.TryParse(segment, out var index) &&
            index >= 0)
        {
            var i = 0;
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (i == index)
                {
                    value = item;
                    return true;
                }
                i++;
            }
        }

        return false;
    }

    private static bool TryGetCaseInsensitive<TValue>(
        IReadOnlyDictionary<string, TValue> dictionary,
        string key,
        out TValue? value)
    {
        if (dictionary.TryGetValue(key, out value))
            return true;

        foreach (var (existingKey, existingValue) in dictionary)
        {
            if (existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = existingValue;
                return true;
            }
        }

        value = default;
        return false;
    }
}