using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Services.Auth;

namespace OrderService.StartupExtensions;

public static class ServiceRegistration
{
    public static void AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // Enable retry on failure for transient fault handling
                    npgsqlOptions.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null);
                }));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderService, Services.OrderService>();
        services.AddScoped<IAuthService, AuthService>();
        
    }
}