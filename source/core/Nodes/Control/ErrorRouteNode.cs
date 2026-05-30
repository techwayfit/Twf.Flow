using Twf.Flow.Core;
using Twf.Flow.Nodes.IO;

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

    private readonly string _errorMessageKey;
    private readonly string _statusCodeKey;
    private readonly int _errorStatusThreshold;

    public ErrorRouteNode(
        string name = "ErrorRoute",
        string errorMessageKey = WorkflowDataKeys.ErrorRoute.InputErrorMessage,
        string statusCodeKey = HttpRequestNode.OutputStatusCode,
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
            NodeParameters.GetString(parameters, "errorMessageKey")  ?? WorkflowDataKeys.ErrorRoute.InputErrorMessage,
            NodeParameters.GetString(parameters, "statusCodeKey")    ?? HttpRequestNode.OutputStatusCode,
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
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Control.Route, route);
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Control.StatusCode, hasStatusCode ? statusCode : -1);
        nodeCtx.SetMetadata(WorkflowDataKeys.Metadata.Control.Threshold, _errorStatusThreshold);

        return Task.FromResult(input.Clone()
            .Set(WorkflowDataKeys.ErrorRoute.ErrorRouteKey, route)
            .Set(WorkflowDataKeys.ErrorRoute.RouteError, isError)
            .Set(WorkflowDataKeys.ErrorRoute.RouteSuccess, !isError)
            .Set(WorkflowDataKeys.ErrorRoute.RoutedErrorMessage, errorMessage));
    }

    public static ErrorRouteNode HttpStatusAware(
        string name = "ErrorRoute",
        string statusCodeKey = "http_status_code",
        int errorStatusThreshold = 400)
    {
        return new ErrorRouteNode(
            name: name,
            errorMessageKey: WorkflowDataKeys.ErrorRoute.InputErrorMessage,
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