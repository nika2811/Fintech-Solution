using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public class OrderService(
    IOrderRepository orderRepository,
    ILogger<IOrderService> logger) : IOrderService
{
    private const decimal DailyLimit = 10000m;
    
    
    public async Task<Order> CreateOrderAsync(Guid companyId, decimal amount, string currency)
    {
        await ValidateOrderLimitAsync(companyId, amount);
        
        var order = new Order(companyId, amount, currency);
        var savedOrder = await orderRepository.AddOrderAsync(order);

        logger.LogInformation("Order {OrderId} created and OrderCreatedEvent published.", savedOrder.OrderId);

        return savedOrder;
    }

    public async Task<IEnumerable<Order>> GetOrdersByCompanyIdAsync(Guid companyId)
    {
        logger.LogInformation("Fetching orders for CompanyId: {CompanyId}", companyId);
        return await orderRepository.GetOrdersByCompanyIdAsync(companyId);
    }

    public async Task<decimal> ComputeTotalOrdersAsync(Guid companyId)
    {
        logger.LogInformation("Computing total completed orders for CompanyId: {CompanyId}", companyId);
        return await orderRepository.GetTotalCompletedOrderAmountForCompanyToday(companyId);
    }

    public async Task<Order> GetOrderByIdAsync(Guid orderId)
    {
        logger.LogInformation("Fetching Order with ID: {OrderId}", orderId);
        return await orderRepository.GetOrderByIdAsync(orderId);
    }
    
    private async Task ValidateOrderLimitAsync(Guid companyId, decimal amount)
    {
        var totalAmountToday = await orderRepository.GetTotalCompletedOrderAmountForCompanyToday(companyId);
        if (totalAmountToday + amount > DailyLimit)
        {
            logger.LogWarning(
                "Daily limit exceeded for CompanyId: {CompanyId}. Current Total: {TotalAmountToday}, Attempted Amount: {Amount}",
                companyId, totalAmountToday, amount);
            throw new InvalidOperationException($"Daily limit of {DailyLimit} exceeded for company {companyId}");
        }
    }
}