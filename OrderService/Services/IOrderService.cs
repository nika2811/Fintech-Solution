using OrderService.Models;

namespace OrderService.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Guid companyId, decimal amount, string currency);
    Task<IEnumerable<Order>> GetOrdersByCompanyIdAsync(Guid companyId);
    Task<decimal> ComputeTotalOrdersAsync(Guid companyId);
    Task<Order> GetOrderByIdAsync(Guid orderId);
}