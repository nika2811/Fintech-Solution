using System.Net;
using Consul;
using Microsoft.Extensions.Options;

namespace IdentityService.StartupExtensions.Consul;

/// <summary>
///     Manages service registration and deregistration with Consul.
/// </summary>
public sealed class ConsulServiceManager : IHostedService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceManager> _logger;
    private readonly ConsulServiceOptions _options;
    private readonly string _registrationId;

    public ConsulServiceManager(
        IConsulClient consulClient,
        IOptions<ConsulServiceOptions> options,
        IHostApplicationLifetime applicationLifetime,
        ILogger<ConsulServiceManager> logger)
    {
        _consulClient = consulClient ?? throw new ArgumentNullException(nameof(consulClient));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registrationId = $"{_options.ServiceName}-{Guid.NewGuid()}";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateOptions(_options);

        using (_logger.BeginScope(new { _options.ServiceName, RegistrationId = _registrationId }))
        {
            try
            {
                await CleanupStaleRegistrationsAsync(cancellationToken);
                await RegisterServiceAsync(cancellationToken);

                _applicationLifetime.ApplicationStopping.Register(() =>
                {
                    var stoppingTask = DeregisterServiceAsync(CancellationToken.None);
                    stoppingTask.ConfigureAwait(false);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Consul service manager");
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return DeregisterServiceAsync(cancellationToken);
    }

    private async Task RegisterServiceAsync(CancellationToken cancellationToken)
    {
        var registration = CreateServiceRegistration();
        try
        {
            await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
            _logger.LogInformation("Registered service: {ServiceName}, ID: {RegistrationId}", _options.ServiceName,
                _registrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service: {RegistrationId}", _registrationId);
            throw;
        }
    }

    private AgentServiceRegistration CreateServiceRegistration()
    {
        return new AgentServiceRegistration
        {
            ID = _registrationId,
            Name = _options.ServiceName,
            Address = Dns.GetHostName(),
            Port = _options.ServicePort,
            Tags = _options.Tags,
            Check = new AgentServiceCheck
            {
                HTTP = _options.HealthCheckUrl,
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };
    }

    private async Task CleanupStaleRegistrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var services = await _consulClient.Agent.Services(cancellationToken);
            var staleServices = services.Response
                .Where(s => s.Value.Service == _options.ServiceName)
                .Select(s => s.Value.ID);

            foreach (var serviceId in staleServices)
            {
                await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken);
                _logger.LogInformation("Deregistered stale service: {ServiceId}", serviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during stale registration cleanup");
        }
    }

    private async Task DeregisterServiceAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_registrationId)) return;

        try
        {
            await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
            _logger.LogInformation("Deregistered service: {RegistrationId}", _registrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service: {RegistrationId}", _registrationId);
        }
    }

    private static void ValidateOptions(ConsulServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new InvalidOperationException("Service name must be configured.");

        if (string.IsNullOrWhiteSpace(options.HealthCheckUrl))
            throw new InvalidOperationException("Health check URL must be configured.");
    }
}