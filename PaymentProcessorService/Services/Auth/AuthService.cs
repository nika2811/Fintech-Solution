using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PaymentProcessorService.DTO;
using Polly;
using Polly.Retry;

namespace PaymentProcessorService.Services.Auth;

public class AuthService(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthService> logger,
    IConfiguration configuration,
    IMemoryCache cache)
    : IAuthService
{
    private const int CacheDurationMinutes = 5;
    private const int SlidingExpirationMinutes = 2;
    private const int RetryCount = 3;
    
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, timeSpan, retryCount, context) =>
            {
                logger.LogWarning("Retry {RetryCount} for IdentityService due to {ExceptionType}: {Message}",
                    retryCount, exception.GetType().Name, exception.Message);
            });

    public async Task<(bool isValid, Guid companyId)> ValidateRequestAsync(string apiKey, string apiSecret)
    {
        var cacheKey = ComputeCacheKey(apiKey, apiSecret);

        if (cache.TryGetValue(cacheKey, out Guid companyId)) return (true, companyId);

        companyId = await ValidateWithIdentityServiceAsync(apiKey, apiSecret);
        if (companyId == Guid.Empty) return (false, Guid.Empty);

        SetCache(cacheKey, companyId);
        
        return (true, companyId);
    }
    
    public async Task<ProcessPaymentDto?> ParseRequestBodyAsync(HttpContext context)
    {
        try
        {
            var processPaymentDto = await context.Request.ReadFromJsonAsync<ProcessPaymentDto>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return processPaymentDto;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON format: {Message}", ex.Message);
            return null;
        }
        
    }

    private async Task<Guid> ValidateWithIdentityServiceAsync(string apiKey, string apiSecret)
    {
        var identityServiceUrl = configuration["IdentityService:ValidateUrl"];
        if (string.IsNullOrWhiteSpace(identityServiceUrl))
        {
            logger.LogError("IdentityService:ValidateUrl is not configured.");
            throw new InvalidOperationException("Internal Server Error");
        }
        
        var requestUri = $"{identityServiceUrl}/Companies/validate";
        var requestBody = new { ApiKey = apiKey, ApiSecret = apiSecret };
        
        try
        {
            using var client = httpClientFactory.CreateClient();
            
            using var response = await _retryPolicy.ExecuteAsync(async () =>
                await client.PostAsJsonAsync(requestUri, requestBody)
            );

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Identity Service returned non-success status code: {StatusCode}",
                    response.StatusCode);
                return Guid.Empty;
            }

            var identityResponse = await response.Content.ReadFromJsonAsync<IdentityResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return identityResponse?.CompanyId ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during request to Identity Service");
            throw new InvalidOperationException("Internal Server Error", ex);
        }
    }
    private void SetCache(string cacheKey, Guid companyId)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(SlidingExpirationMinutes)
        };

        cache.Set(cacheKey, companyId, cacheEntryOptions);
    }

    private static string ComputeCacheKey(string apiKey, string apiSecret)
    {
        var inputBytes = Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}");
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }
}