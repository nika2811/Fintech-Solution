using PaymentProcessorService.DTO;

namespace PaymentProcessorService.Services.Auth;

public interface IAuthService
{
    Task<(bool isValid, Guid companyId)> ValidateRequestAsync(string apiKey, string apiSecret);
    Task<ProcessPaymentDto?> ParseRequestBodyAsync(HttpContext context);
}