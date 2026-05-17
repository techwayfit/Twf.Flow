using System.Text.Json;
using System.Text.RegularExpressions;
using Twf.Flow.Core;
using Twf.Flow.Nodes;

namespace Twf.Flow.Web.Services.VariableResolution;

/// <summary>
/// Resolves template variables using {{variable}} syntax.
/// Supports nested key paths (e.g., {{node.key}}) and respects excluded credential keys.
/// </summary>
public class TemplateVariableResolver : IVariableResolver
{
    /// <summary>
    /// Parameter keys whose values are never treated as {{variable}} templates.
    /// Only block keys where the stored value is a literal secret that should
    /// never be accidentally expanded — not where the user intentionally typed
    /// a {{variable}} reference to inject a secret from workflow variables.
    /// </summary>
    private readonly HashSet<string> _noResolveKeys = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
    /// Regex pattern for matching {{variable}} placeholders.
    /// Compiled for better performance.
    /// </summary>
    private static readonly Regex _variablePattern = new(@"\{\{([\w.]+)\}\}", RegexOptions.Compiled);

    public Dictionary<string, object?> ResolveParameters(
        Dictionary<string, object?> parameters,
    WorkflowData workflowData)
    {
      var result = new Dictionary<string, object?>(
      parameters.Count,
StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in parameters)
  {
    // Skip resolution for credential keys
       if (_noResolveKeys.Contains(key))
      {
   result[key] = value;
  continue;
     }

    // Resolve based on value type
 result[key] = value switch
         {
        string str => ResolveVariables(str, workflowData),
      JsonElement { ValueKind: JsonValueKind.String } je =>
   ResolveVariables(je.GetString() ?? "", workflowData),
        _ => value
    };
   }

 return result;
    }

    public string ResolveVariables(string template, WorkflowData workflowData)
    {
        if (string.IsNullOrEmpty(template))
  return template;

        return _variablePattern.Replace(template, match =>
        {
 var variablePath = match.Groups[1].Value;

  // Build a dictionary for nested value resolution
var dataDict = workflowData.Keys.ToDictionary(
    k => k,
     k => workflowData.Get<object>(k));

      // Try to get the nested value
  var value = NodeParameters.GetNestedValue(dataDict, variablePath);

   // Return the value as string, or keep the original placeholder if not found
          return value?.ToString() ?? match.Value;
     });
    }

    public void RegisterNoResolveKey(string parameterKey)
    {
        if (!string.IsNullOrWhiteSpace(parameterKey))
      {
     _noResolveKeys.Add(parameterKey);
}
    }
}
