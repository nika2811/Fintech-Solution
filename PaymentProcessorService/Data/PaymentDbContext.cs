using Microsoft.EntityFrameworkCore;
using PaymentProcessorService.Models;

namespace PaymentProcessorService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments { get; set; }
}