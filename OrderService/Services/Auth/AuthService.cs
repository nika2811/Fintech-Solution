using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OrderService.DTO;
using OrderService.Middleware;

namespace OrderService.Services.Auth;

public class AuthService(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthService> logger,
    IConfiguration configuration,
    IMemoryCache cache)
    : IAuthService
{
    private const int CacheDurationMinutes = 5;
    private const string InvalidCompanyIdError = "Missing or invalid CompanyId";
    private const string InternalServerError = "Internal Server Error";

    public async Task<bool> ValidateRequestAsync(string apiKey, string apiSecret, CreateOrderDto dto)
    {
        if (!IsValidDto(dto))
        {
            logger.LogWarning(InvalidCompanyIdError);
            return false;
        }

        var cacheKey = GetHashedCacheKey(apiKey, apiSecret, dto.CompanyId.ToString());

        if (cache.TryGetValue(cacheKey, out bool isValid) && isValid) return true;

        isValid = await ValidateWithIdentityServiceAsync(apiKey, apiSecret, dto.CompanyId);
        
        if (isValid) cache.Set(cacheKey, true, TimeSpan.FromMinutes(CacheDurationMinutes));

        return isValid;
    }
    private static bool IsValidDto(CreateOrderDto dto) =>
        dto != null && dto.CompanyId != Guid.Empty;

    private async Task<bool> ValidateWithIdentityServiceAsync(string apiKey, string apiSecret, Guid companyId)
    {
        var identityServiceUrl = configuration["IdentityService:ValidateUrl"];
        if (string.IsNullOrWhiteSpace(identityServiceUrl))
        {
            logger.LogError("IdentityService:ValidateUrl is not configured.");
            return false;
        }

        var client = httpClientFactory.CreateClient();
        var requestUri = $"{identityServiceUrl}/Companies/{companyId}?apiKey={apiKey}&apiSecret={apiSecret}";

        try
        {

            var response = await client.GetFromJsonAsync<IdentityResponse>(requestUri);

            return response != null && response.ApiKey == apiKey && response.ApiSecret == apiSecret;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during request to Identity Service");
            throw new InvalidOperationException(InternalServerError, ex);
        }
    }

    private static string GetHashedCacheKey(string apiKey, string apiSecret, string additionalInfo)
    {
        var keyBytes = Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}:{additionalInfo}");
        var hashBytes = SHA256.HashData(keyBytes);
        return Convert.ToBase64String(hashBytes);
    }
}