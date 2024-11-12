using MassTransit;
using OrderService.Models;
using OrderService.Repositories;
using Shared.Events.Payment;

namespace OrderService.Consumers;

public class PaymentProcessedEventConsumer(
    IOrderRepository orderRepository,
    ILogger<PaymentProcessedEventConsumer> logger)
    : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        try
        {
            var message = context.Message;
            logger.LogInformation("Received PaymentProcessedEvent for OrderId: {OrderId} with Status: {Status}",
                message.OrderId, message.Status);

            var order = await orderRepository.GetOrderByIdAsync(message.OrderId);

            if (order == null)
            {
                logger.LogWarning("Order with ID {OrderId} not found.", message.OrderId);
                return;
            }

            // Update the order status based on the payment status
            order.Status = message.Status == PaymentStatus.Completed ? OrderStatus.Completed : OrderStatus.Rejected;

            await orderRepository.UpdateOrderAsync(order);

            logger.LogInformation("Order ID {OrderId} updated to status {Status}.", order.OrderId, order.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PaymentProcessedEvent for OrderId: {OrderId}",
                context.Message.OrderId);
            // Optionally, implement retry logic or move to a dead-letter queue
            throw;
        }
    }
}