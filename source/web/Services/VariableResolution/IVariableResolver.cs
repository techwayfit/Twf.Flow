using Twf.Flow.Core;

namespace Twf.Flow.Web.Services.VariableResolution;

/// <summary>
/// Service responsible for resolving template variables in workflow parameters.
/// Handles {{variable}} substitution and supports nested key paths.
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    /// Resolves all {{variable}} placeholders in a parameter dictionary.
    /// </summary>
    /// <param name="parameters">The raw parameters with potential {{variable}} placeholders.</param>
    /// <param name="workflowData">The workflow data containing variable values.</param>
    /// <returns>A new dictionary with all variables resolved to their actual values.</returns>
    Dictionary<string, object?> ResolveParameters(
        Dictionary<string, object?> parameters,
     WorkflowData workflowData);

    /// <summary>
    /// Resolves {{variable}} placeholders in a single string value.
    /// </summary>
    /// <param name="template">The string with potential {{variable}} placeholders.</param>
    /// <param name="workflowData">The workflow data containing variable values.</param>
    /// <returns>The string with all variables resolved to their actual values.</returns>
    string ResolveVariables(string template, WorkflowData workflowData);

    /// <summary>
    /// Registers a parameter key that should never have variable resolution applied.
 /// Useful for credential fields that should remain literal.
    /// </summary>
    /// <param name="parameterKey">The parameter key to exclude from resolution.</param>
    void RegisterNoResolveKey(string parameterKey);
}
