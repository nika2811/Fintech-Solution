using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityService.Middleware;

public class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger,
    IHostEnvironment env,
    ProblemDetailsFactory problemDetailsFactory)
{
    private readonly IHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));

    private readonly ILogger<ErrorHandlingMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    private readonly ProblemDetailsFactory _problemDetailsFactory =
        problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogWarning(oce, "Request was canceled.");
            // Optionally, do nothing or handle the cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred while processing the request.");

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "The response has already started, the error handling middleware will not be executed.");
                throw;
            }

            var problemDetails = CreateProblemDetails(context, ex);

            context.Response.Clear();
            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, options);
            await context.Response.Body.FlushAsync();
        }
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var statusCode = MapExceptionToStatusCode(exception);

        var problemDetails = _problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode,
            ReasonPhrases.GetReasonPhrase(statusCode),
            detail: _env.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.",
            instance: context.Request.Path
        );

        return problemDetails;
    }

    private static int MapExceptionToStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}