using Microsoft.EntityFrameworkCore;
using PaymentProcessorService.Data;
using PaymentProcessorService.Models;

namespace PaymentProcessorService.Repositories;

public class PaymentRepository(PaymentDbContext context) : IPaymentRepository
{
    public async Task<Payment> CreateAsync(Payment payment)
    {
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> GetByIdAsync(Guid paymentId)
    {
        return await context.Payments.FindAsync(paymentId);
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await context.Payments.ToListAsync();
    }
}