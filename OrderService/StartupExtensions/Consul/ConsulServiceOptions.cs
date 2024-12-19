namespace OrderService.StartupExtensions.Consul;

public class ConsulServiceOptions
{
    /// <summary>
    ///     The name of the service to be registered.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     The port on which the service is running.
    /// </summary>
    public int ServicePort { get; set; } = 8082;

    /// <summary>
    ///     The URL used for health checks.
    /// </summary>
    public string HealthCheckUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The address of the Consul server.
    /// </summary>
    public string Address { get; set; } = "http://localhost:8500";

    /// <summary>
    ///     The Tags of the service.
    /// </summary>
    public string[] Tags { get; set; } = [];
}