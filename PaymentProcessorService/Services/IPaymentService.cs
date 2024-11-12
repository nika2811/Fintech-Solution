using PaymentProcessorService.Models;

namespace PaymentProcessorService.Services;

public interface IPaymentService
{
    // Task<Payment> ProcessPaymentAsync(Payment payment);
    Task<Payment> ProcessPaymentAsync(Guid orderId, string cardNumber, DateTime expiryDate, Guid companyId);
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();
}