using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace OrderService.StartupExtensions.RateLimiter;

public static class RateLimiterConfiguration
{
    public static IServiceCollection ConfigureRateLimiter(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimiterSettings>()
            .Bind(configuration.GetSection(RateLimiterSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options => ConfigureRateLimiter(options, services));
        services.AddMemoryCache();

        return services;
    }

    private static void ConfigureRateLimiter(RateLimiterOptions options, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptions<RateLimiterSettings>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<RateLimiterOptions>>();

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext => CreateRateLimitPartition(httpContext, settings));

        options.OnRejected = async (context, cancellationToken) =>
            await HandleRejectedRequest(context, settings, logger, cancellationToken);

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("fixed", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                "global_fixed",
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = settings.UnauthenticatedPermitLimit,
                    QueueLimit = settings.QueueLimit,
                    Window = settings.Window
                }));
    }

    private static RateLimitPartition<string> CreateRateLimitPartition(HttpContext httpContext,
        RateLimiterSettings settings)
    {
        var (partitionKey, permitLimit) = GetPartitionDetails(httpContext, settings);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                QueueLimit = settings.QueueLimit,
                Window = settings.Window
            });
    }

    private static (string Key, int PermitLimit) GetPartitionDetails(HttpContext httpContext,
        RateLimiterSettings settings)
    {
        var identity = httpContext.User.Identity;
        var isAuthenticated = identity?.IsAuthenticated ?? false;

        if (isAuthenticated && !string.IsNullOrEmpty(identity?.Name))
            return (identity.Name, settings.AuthenticatedPermitLimit);

        var ipAddress = GetClientIpAddress(httpContext);
        return (ipAddress ?? settings.AnonymousKey, settings.UnauthenticatedPermitLimit);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static async ValueTask HandleRejectedRequest(OnRejectedContext context, RateLimiterSettings settings,
        ILogger logger, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var retryAfter = DateTime.UtcNow.Add(settings.Window);
        logger.LogWarning("Rate limit exceeded for {IpAddress}. User: {User}. Endpoint: {Endpoint}",
            GetClientIpAddress(httpContext), httpContext.User.Identity?.Name ?? "anonymous", httpContext.Request.Path);

        var response = new
        {
            Error = "Too many requests. Please try again later.",
            RetryAfter = retryAfter,
            Details = $"Rate limit exceeded. Please try again after {retryAfter:HH:mm:ss UTC}"
        };

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.RetryAfter = retryAfter.ToString("R");
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    }
}