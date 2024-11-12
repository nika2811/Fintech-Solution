using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public class OrderRepository(OrderDbContext dbContext, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order> AddOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);


            await dbContext.Orders.AddAsync(order).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Added new Order with ID {OrderId}", order.OrderId);
            return order;
    }

    public async Task UpdateOrderAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        
            dbContext.Orders.Update(order);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Updated Order with ID {OrderId} to status {Status}", order.OrderId, order.Status);
        
    }

    public async Task<IEnumerable<Order>> GetOrdersByCompanyIdAsync(Guid companyId)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Order>> GetOrdersByCompanyIdForDateAsync(Guid companyId, DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<Order> GetOrderByIdAsync(Guid orderId)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId)
            .ConfigureAwait(false);
        
        if (order == null)
        {
            logger.LogWarning("Order with ID {OrderId} not found", orderId);
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        return order;
    }

    public async Task<decimal> GetTotalCompletedOrderAmountForCompanyToday(Guid companyId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await dbContext.Orders
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= today && o.CreatedAt < tomorrow &&
                        o.Status == OrderStatus.Completed)
            .SumAsync(o => o.Amount)
            .ConfigureAwait(false);
    }
}