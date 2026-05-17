namespace Twf.Flow.Web.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for tracking across logs and services.
/// If a correlation ID is provided in the request header, it is used; otherwise, a new one is generated.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
 private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
 RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
   _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrCreateCorrelationId(context);

   // Store in HttpContext.Items for access by other middleware/handlers
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers for client-side tracking
 context.Response.Headers[CorrelationIdHeader] = correlationId;

   // Add to logging scope for structured logging
        using (_logger.BeginScope(new Dictionary<string, object>
      {
        ["CorrelationId"] = correlationId,
   ["RequestPath"] = context.Request.Path.ToString(),
 ["RequestMethod"] = context.Request.Method,
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
    }))
  {
      await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
   // Check if correlation ID is provided in request header
if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) 
            && !string.IsNullOrWhiteSpace(headerValue))
        {
        return headerValue.ToString();
  }

  // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}
