using PaymentProcessorService.Models;

namespace PaymentProcessorService.Repositories;

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> GetByIdAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetAllAsync();
}