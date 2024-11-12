using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.OrderId);

            entity.Property(o => o.CompanyId)
                .IsRequired();

            entity.Property(o => o.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(o => o.Status)
                .IsRequired();

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            // Optional: Add indexes for frequently queried fields
            entity.HasIndex(o => o.CompanyId);
            entity.HasIndex(o => o.Status);
        });
    }
}