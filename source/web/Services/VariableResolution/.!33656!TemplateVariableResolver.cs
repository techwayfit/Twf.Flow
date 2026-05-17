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
