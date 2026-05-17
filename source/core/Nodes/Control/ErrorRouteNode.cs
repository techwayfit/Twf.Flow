using Twf.Flow.Core;

namespace Twf.Flow.Nodes.Control;
// ═══════════════════════════════════════════════════════════════════════════════
// ErrorRouteNode — Routes data to success/error based on error indicators
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Detects whether current WorkflowData indicates an error and writes explicit
/// routing flags for downstream conditional branching.
///
/// Error detection rules:
///   1. Non-empty error message in <see cref="_errorMessageKey"/>
///   2. HTTP status code in <see cref="_statusCodeKey"/> >= <see cref="_errorStatusThreshold"/>
///
/// Writes:
///   - "error_route"   : "error" | "success"
///   - "route_error"   : bool
///   - "route_success" : bool
///   - "routed_error_message" : optional synthesized message when route is error
/// </summary>
public sealed class ErrorRouteNode : BaseNode
{
    public override string Name { get; }
    public override string Category => "Control";
    public override string Description =>
        $"Routes by error indicators (message='{_errorMessageKey}', status='{_statusCodeKey}')";

    /// <summary>UI schema: parameter form fields shown in the properties panel.</summary>
    public static NodeParameterSchema Schema { get; } = new()
    {
        NodeType    = "ErrorRouteNode",
        Description = "Detect errors and route to success or error path",
        Parameters  =
        [
            new() { Name = "errorMessageKey",     Label = "Error Message Key",     Type = ParameterType.Text,   Required = false, DefaultValue = "error_message",
                Description = "WorkflowData key that holds an error message string" },
            new() { Name = "statusCodeKey",       Label = "Status Code Key",       Type = ParameterType.Text,   Required = false, DefaultValue = "http_status_code",
                Description = "WorkflowData key that holds an HTTP status code" },
            new() { Name = "errorStatusThreshold",Label = "Error Status Threshold",Type = ParameterType.Number, Required = false, DefaultValue = 400, MinValue = 100, MaxValue = 599,
                Description = "Status codes >= this value are treated as errors" },
        ]
    };

    private readonly string _errorMessageKey;
    private readonly string _statusCodeKey;
    private readonly int _errorStatusThreshold;

    public ErrorRouteNode(
        string name = "ErrorRoute",
        string errorMessageKey = "error_message",
        string statusCodeKey = "http_status_code",
        int errorStatusThreshold = 400)
    {
        Name = name;
        _errorMessageKey = errorMessageKey;
        _statusCodeKey = statusCodeKey;
        _errorStatusThreshold = errorStatusThreshold;
    }

    /// <summary>Dictionary constructor for dynamic instantiation.</summary>
    public ErrorRouteNode(Dictionary<string, object?> parameters)
        : this(
            NodeParameters.GetString(parameters, "name") ?? "Error Route",
            NodeParameters.GetString(parameters, "errorMessageKey")  ?? "error_message",
            NodeParameters.GetString(parameters, "statusCodeKey")    ?? "http_status_code",
            NodeParameters.GetInt(parameters,    "errorStatusThreshold", 400))
    { }

    protected override Task<WorkflowData> RunAsync(
        WorkflowData input, WorkflowContext context, NodeExecutionContext nodeCtx)
    {
        var errorMessage = input.GetString(_errorMessageKey);
        var hasErrorMessage = !string.IsNullOrWhiteSpace(errorMessage);

        var hasStatusCode = TryGetStatusCode(input, _statusCodeKey, out var statusCode);
        var isErrorStatus = hasStatusCode && statusCode >= _errorStatusThreshold;

        var isError = hasErrorMessage || isErrorStatus;
        var route = isError ? "error" : "success";

        if (isError && string.IsNullOrWhiteSpace(errorMessage) && isErrorStatus)
            errorMessage = $"HTTP status {statusCode} (threshold {_errorStatusThreshold})";

        nodeCtx.Log(
            $"Error route={route} (hasErrorMessage={hasErrorMessage}, hasStatusCode={hasStatusCode}, statusCode={(hasStatusCode ? statusCode : 0)})");
        nodeCtx.SetMetadata("route", route);
        nodeCtx.SetMetadata("status_code", hasStatusCode ? statusCode : -1);
        nodeCtx.SetMetadata("threshold", _errorStatusThreshold);

        return Task.FromResult(input.Clone()
            .Set("error_route", route)
            .Set("route_error", isError)
            .Set("route_success", !isError)
            .Set("routed_error_message", errorMessage));
    }

    public static ErrorRouteNode HttpStatusAware(
        string name = "ErrorRoute",
        string statusCodeKey = "http_status_code",
        int errorStatusThreshold = 400)
    {
        return new ErrorRouteNode(
            name: name,
            errorMessageKey: "error_message",
            statusCodeKey: statusCodeKey,
            errorStatusThreshold: errorStatusThreshold);
    }

    private static bool TryGetStatusCode(WorkflowData input, string key, out int statusCode)
    {
        statusCode = 0;
        if (!input.Has(key))
            return false;

        var raw = input.Get<object>(key);
        if (raw is null)
            return false;

        if (raw is int intValue)
        {
            statusCode = intValue;
            return true;
        }

        return int.TryParse(raw.ToString(), out statusCode);
    }
}