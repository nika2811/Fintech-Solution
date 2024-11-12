using Microsoft.EntityFrameworkCore;
using PaymentProcessorService.Data;
using PaymentProcessorService.ExternalServices;
using PaymentProcessorService.Repositories;
using PaymentProcessorService.Services;
using PaymentProcessorService.Services.Auth;

namespace PaymentProcessorService.StartupExtensions;

public static class ServiceRegistration
{
    public static void AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // Enable retry on failure for transient fault handling
                    npgsqlOptions.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null);
                }));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IExternalPaymentService, ServiceA>();
        services.AddScoped<IExternalPaymentService, ServiceB>();
    }
}