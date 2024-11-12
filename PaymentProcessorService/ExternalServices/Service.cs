namespace PaymentProcessorService.ExternalServices;

public class ServiceA : IExternalPaymentService
{
    public Task<bool> ProcessPaymentAsync(Guid orderId, string cardNumber, DateTime expiryDate)
    {
        Console.WriteLine($"Processing payment via ServiceA for Order: {orderId}");
        return Task.FromResult(new Random().Next(0, 2) == 1); // Simulate random success/failure
    }
}

public class ServiceB : IExternalPaymentService
{
    public Task<bool> ProcessPaymentAsync(Guid orderId, string cardNumber, DateTime expiryDate)
    {
        Console.WriteLine($"Processing payment via ServiceB for Order: {orderId}");
        return Task.FromResult(new Random().Next(0, 2) == 1); // Simulate random success/failure
    }
}