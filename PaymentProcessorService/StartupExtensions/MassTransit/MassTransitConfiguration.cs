using MassTransit;
using Microsoft.Extensions.Options;
using OrderService.StartupExtensions.MassTransit;

namespace PaymentProcessorService.StartupExtensions.MassTransit;

public static class MassTransitConfiguration
{
    public static void AddMassTransitServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MessageBrokerSettings>()
            .Bind(configuration.GetSection(MessageBrokerSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;

                configurator.Host(new Uri(configuration["MessageBroker:Host"]!), h =>
                {
                    h.Username(settings.Username);
                    h.Password(settings.Password);
                    // if (settings.UseSSL)
                    // {
                    //     h.UseSsl(s =>
                    //     {
                    //         s.Protocol = SslProtocols.Tls12;
                    //     });
                    // }
                });

                configurator.UseMessageRetry(r =>
                {
                    r.Incremental(settings.RetryCount,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2));

                    r.Handle<RabbitMqConnectionException>();
                    r.Handle<OperationCanceledException>();
                });

                // Configure concurrent message limit
                configurator.PrefetchCount = settings.ConcurrentMessageLimit;

                // Add circuit breaker
                configurator.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });
                configurator.ConfigureEndpoints(context);
            });
        });
    }
}