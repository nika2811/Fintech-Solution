using Consul;
using Microsoft.Extensions.Options;

namespace PaymentProcessorService.StartupExtensions.Consul;

public static class ConsulServiceExtensions
{
    public static void AddConsulServiceDiscovery(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConsulServiceOptions>(configuration.GetSection("Consul"));

        services.AddSingleton<IConsulClient>(sp =>
            new ConsulClient(config =>
            {
                var consulOptions = sp.GetRequiredService<IOptions<ConsulServiceOptions>>().Value;
                config.Address = new Uri(consulOptions.Address);
            }));

        services.AddHostedService<ConsulServiceManager>();
    }
}