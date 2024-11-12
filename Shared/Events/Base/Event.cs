namespace Shared.Events.Base;

public abstract record Event(string EventSource)
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventSource { get; } = EventSource;
}