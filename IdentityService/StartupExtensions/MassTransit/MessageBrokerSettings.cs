namespace IdentityService.StartupExtensions.MassTransit;

public class MessageBrokerSettings
{
    public const string SectionName = "MessageBroker";

    public required string Host { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public ushort RetryCount { get; init; } = 3;
    public ushort ConcurrentMessageLimit { get; init; } = 10;
    public bool UseSSL { get; init; } = true;
}