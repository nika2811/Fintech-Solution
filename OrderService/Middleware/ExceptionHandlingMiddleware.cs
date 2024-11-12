using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IHostEnvironment env,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception has occurred while processing the request.");

            context.Response.ContentType = "application/json";

            var problemDetails = CreateProblemDetails(context, ex);

            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            var json = JsonSerializer.Serialize(problemDetails, options);

            await context.Response.WriteAsync(json);
        }
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            NotImplementedException => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitle(statusCode),
            Detail = env.IsDevelopment() ? exception.StackTrace : "An unexpected error occurred.",
            Instance = context.Request.Path
        };

        return problemDetails;
    }

    private static string GetTitle(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.NotImplemented => "Not Implemented",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            _ => "Error"
        };
    }
}