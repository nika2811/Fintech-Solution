using OrderService.Models;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> AddOrderAsync(Order order);
    Task UpdateOrderAsync(Order order);
    Task<IEnumerable<Order>> GetOrdersByCompanyIdAsync(Guid companyId);
    Task<IEnumerable<Order>> GetOrdersByCompanyIdForDateAsync(Guid companyId, DateTime date);
    Task<Order> GetOrderByIdAsync(Guid orderId);
    Task<decimal> GetTotalCompletedOrderAmountForCompanyToday(Guid companyId);
}