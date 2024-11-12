using Shared.Events.Payment;

namespace PaymentProcessorService.Models;

public class Payment(Guid orderId, string cardNumber, DateTime expiryDate)
{
    public Guid PaymentId { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; } = orderId;
    public string CardNumber { get; private set; } = cardNumber;
    public DateTime ExpiryDate { get; private set; } = expiryDate;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Payment MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        return this;
    }

    public Payment MarkAsRejected()
    {
        Status = PaymentStatus.Rejected;
        return this;
    }
}