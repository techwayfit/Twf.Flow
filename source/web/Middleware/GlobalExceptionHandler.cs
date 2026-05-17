using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Twf.Flow.Web.Middleware;

/// <summary>
/// Global exception handler that provides consistent error responses across the application.
/// Implements IExceptionHandler to integrate with ASP.NET Core's exception handling pipeline.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
ILogger<GlobalExceptionHandler> logger,
   IHostEnvironment environment)
    {
      _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
  CancellationToken cancellationToken)
    {
   // Get correlation ID if available
        var correlationId = httpContext.Items["CorrelationId"]?.ToString() 
            ?? Activity.Current?.Id 
?? httpContext.TraceIdentifier;

   // Log the exception with context
        _logger.LogError(
            exception,
            "Unhandled exception occurred. Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}",
       httpContext.Request.Path,
            httpContext.Request.Method,
          correlationId);

        // Create appropriate ProblemDetails based on exception type
        var problemDetails = CreateProblemDetails(exception, httpContext, correlationId);

        // Set response status code
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

     // Write ProblemDetails as JSON
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to indicate the exception was handled
        return true;
    }

 private ProblemDetails CreateProblemDetails(
   Exception exception,
   HttpContext httpContext,
        string correlationId)
    {
        return exception switch
     {
 // Validation errors (e.g., from FluentValidation)
          ArgumentException or ArgumentNullException => new ProblemDetails
     {
      Status = StatusCodes.Status400BadRequest,
   Title = "Bad Request",
    Detail = _environment.IsDevelopment() ? exception.Message : "Invalid request parameters",
       Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
           Instance = httpContext.Request.Path,
                Extensions =
   {
      ["correlationId"] = correlationId,
    ["timestamp"] = DateTime.UtcNow
 }
        },

            // Resource not found
         KeyNotFoundException or FileNotFoundException => new ProblemDetails
  {
 Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
          Detail = _environment.IsDevelopment() ? exception.Message : "The requested resource was not found",
   Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  Instance = httpContext.Request.Path,
        Extensions =
            {
  ["correlationId"] = correlationId,
   ["timestamp"] = DateTime.UtcNow
    }
       },

 // Invalid operation (business logic errors)
          InvalidOperationException => new ProblemDetails
            {
      Status = StatusCodes.Status422UnprocessableEntity,
         Title = "Unprocessable Entity",
         Detail = _environment.IsDevelopment() ? exception.Message : "The request could not be processed",
      Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
     Instance = httpContext.Request.Path,
       Extensions =
      {
    ["correlationId"] = correlationId,
  ["timestamp"] = DateTime.UtcNow
           }
  },

  // Timeout exceptions
      TimeoutException => new ProblemDetails
{
                Status = StatusCodes.Status504GatewayTimeout,
     Title = "Gateway Timeout",
              Detail = _environment.IsDevelopment() ? exception.Message : "The operation timed out",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
        Instance = httpContext.Request.Path,
        Extensions =
       {
    ["correlationId"] = correlationId,
      ["timestamp"] = DateTime.UtcNow
       }
     },

     // Unauthorized access
     UnauthorizedAccessException => new ProblemDetails
        {
             Status = StatusCodes.Status403Forbidden,
      Title = "Forbidden",
 Detail = _environment.IsDevelopment() ? exception.Message : "Access to this resource is forbidden",
     Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
          Instance = httpContext.Request.Path,
 Extensions =
     {
        ["correlationId"] = correlationId,
        ["timestamp"] = DateTime.UtcNow
                }
         },

   // Default: Internal Server Error
    _ => new ProblemDetails
            {
Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
                Detail = _environment.IsDevelopment() 
               ? exception.Message 
            : "An unexpected error occurred. Please try again later.",
  Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path,
         Extensions =
      {
          ["correlationId"] = correlationId,
               ["timestamp"] = DateTime.UtcNow,
           ["exceptionType"] = exception.GetType().Name
           }
            }
        };
    }
}
