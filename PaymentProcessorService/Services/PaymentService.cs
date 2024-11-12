using System.Text.Json;
using MassTransit;
using PaymentProcessorService.DTO;
using PaymentProcessorService.ExternalServices;
using PaymentProcessorService.Models;
using PaymentProcessorService.Repositories;
using Shared.Events.Payment;

namespace PaymentProcessorService.Services;

public class PaymentService(
    IPublishEndpoint publishEndpoint,
    IExternalPaymentService serviceA,
    IExternalPaymentService serviceB,
    IPaymentRepository paymentRepository,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<PaymentService> logger)
    : IPaymentService
{
    public async Task<Payment> ProcessPaymentAsync(Guid orderId, string cardNumber, DateTime expiryDate, Guid companyId)
    {
        if (!await IsOrderValid(orderId, companyId))
            throw new ArgumentException("Invalid order ID.");

        var payment = new Payment(orderId, cardNumber, expiryDate);

        if (!IsExpiryDateValid(expiryDate))
        {
            payment.MarkAsRejected();
            await SavePaymentAsync(payment);
            return payment;
        }

        var paymentProcessed = await GetPaymentService(cardNumber)
            .ProcessPaymentAsync(orderId, cardNumber, expiryDate);

        payment = paymentProcessed ? payment.MarkAsCompleted() : payment.MarkAsRejected();

        await SavePaymentAsync(payment);
        await PublishPaymentProcessedEvent(payment);

        return payment;
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
    {
        return await paymentRepository.GetByIdAsync(paymentId)
               ?? throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");
    }

    public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
    {
        return await paymentRepository.GetAllAsync();
    }

    private async Task<bool> IsOrderValid(Guid orderId, Guid companyId)
    {
        var orderServiceUrl = configuration["OrderService:Url"];
        if (string.IsNullOrEmpty(orderServiceUrl))
        {
            logger.LogError("orderServiceUrl:ValidateUrl is not configured.");
            return false;
        }

        if (companyId == Guid.Empty)
            throw new UnauthorizedAccessException("Unauthorized: Missing or invalid CompanyId.");

        var client = httpClientFactory.CreateClient();
        var requestUrl = $"{orderServiceUrl}/Order/{orderId}/exists?companyId={companyId}";

        var response = await client.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode) return false;

        var responseContent = await response.Content.ReadAsStringAsync();
        var existsResponse = JsonSerializer.Deserialize<ExistsResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return existsResponse?.Exists ?? false;
    }

    private async Task PublishPaymentProcessedEvent(Payment payment)
    {
        var paymentProcessedEvent = new PaymentProcessedEvent(
            payment.PaymentId,
            payment.OrderId,
            payment.Status,
            nameof(PaymentService)
        );

        await publishEndpoint.Publish(paymentProcessedEvent);
    }

    private IExternalPaymentService GetPaymentService(string cardNumber)
    {
        return IsEven(cardNumber) ? serviceA : serviceB;
    }

    private static bool IsExpiryDateValid(DateTime expiryDate)
    {
        return expiryDate >= DateTime.UtcNow;
    }

    private async Task SavePaymentAsync(Payment payment)
    {
        await paymentRepository.CreateAsync(payment);
    }

    private static bool IsEven(string cardNumber)
    {
        var lastDigit = int.Parse(cardNumber[^1].ToString());
        return lastDigit % 2 == 0;
    }
}