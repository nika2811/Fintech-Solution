namespace PaymentProcessorService.ExternalServices;

public interface IExternalPaymentService
{
    Task<bool> ProcessPaymentAsync(Guid orderId, string cardNumber, DateTime expiryDate);
}