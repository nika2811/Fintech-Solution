using OrderService.DTO;

namespace OrderService.Services.Auth;

public interface IAuthService
{
    Task<bool> ValidateRequestAsync(string apiKey, string apiSecret, CreateOrderDto dto);
}