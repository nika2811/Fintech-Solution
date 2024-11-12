namespace IdentityService.StartupExtensions.RateLimiter;

public class RateLimiterSettings
{
    public const string SectionName = "RateLimiter";

    public required string AnonymousKey { get; init; } = "anonymous";
    public required int AuthenticatedPermitLimit { get; init; } = 100;
    public required int UnauthenticatedPermitLimit { get; init; } = 10;
    public required int QueueLimit { get; init; } = 2;
    public required TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    public required TimeSpan BanDuration { get; init; } = TimeSpan.FromHours(1);
    public required int MaxAllowedExceededAttempts { get; init; } = 5;
}