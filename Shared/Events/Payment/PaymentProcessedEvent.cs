using Shared.Events.Base;

namespace Shared.Events.Payment;

public record PaymentProcessedEvent(Guid PaymentId, Guid OrderId, PaymentStatus Status, string EventSource)
    : Event(EventSource);