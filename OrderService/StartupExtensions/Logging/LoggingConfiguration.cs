using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace OrderService.StartupExtensions.Logging;

public static class LoggingConfiguration
{
    private const string AppName = "IdentityService";
    private const string Version = "1.0.0";

    // Single point of configuration for OpenTelemetry collector
    private static readonly string OtelCollectorEndpoint = Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT") 
        ?? "http://otel-collector:4317";

    public static WebApplicationBuilder AddCentralizedObservability(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Set up ActivitySource and Meter for manual instrumentation
        var activitySource = new ActivitySource(AppName, Version);
        var meter = new Meter(AppName, Version);

        builder.Services.AddSingleton(activitySource);
        builder.Services.AddSingleton(meter);
        builder.Services.AddSingleton<CustomMetrics>();
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(_ => CreateResourceBuilder(builder))
            .WithTracing(ConfigureTracing)
            .WithMetrics(ConfigureMetrics);


        ConfigureSerilog(builder);

        // OpenTelemetry logging configuration
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(CreateResourceBuilder(builder));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.AddOtlpExporter(ConfigureOtlpExporterOptions);
        });
        
        return builder;
    }

    private static ResourceBuilder CreateResourceBuilder(WebApplicationBuilder builder)
    {
        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: AppName, serviceVersion: Version, serviceInstanceId: Environment.MachineName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector()
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["host.name"] = Environment.MachineName,
                ["process.runtime.name"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                ["service.namespace"] = "Identity",
                ["service.instance.id"] = Guid.NewGuid().ToString(),
                ["application"] = AppName,
                ["application.version"] = Version
            });
    }
    private static void ConfigureTracing(TracerProviderBuilder tracing)
        {
            tracing
                .AddSource(AppName)
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1.0)))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        if (httpRequest.Headers.TryGetValue("x-correlation-id", out var correlationId))
                        {
                            activity.SetTag("custom.http.request.header.x-correlation-id", correlationId.ToString());
                        }
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (request.Headers.Contains("x-correlation-id"))
                        {
                            activity.SetTag("custom.http.request.header.x-correlation-id",
                                request.Headers.GetValues("x-correlation-id").FirstOrDefault());
                        }
                    };
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.statement.parameters",
                            string.Join(", ", command.Parameters.Cast<DbParameter>()
                                .Select(p => $"{p.ParameterName}={p.Value}")));
                    };
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.SetDbStatementForText = true;
                    options.EnableConnectionLevelAttributes = true;
                })
                .AddGrpcClientInstrumentation()
                .AddOtlpExporter(ConfigureOtlpExporterOptions);
        }

    private static void ConfigureMetrics(MeterProviderBuilder metrics)
    { 
        metrics
            .AddMeter(AppName)
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEventCountersInstrumentation()
            .AddOtlpExporter(ConfigureOtlpExporterOptions)
            .AddProcessInstrumentation();
    }

    private static void ConfigureOtlpExporterOptions(OtlpExporterOptions options)
    {
        options.Endpoint = new Uri(OtelCollectorEndpoint);
        options.Protocol = OtlpExportProtocol.Grpc;
        options.ExportProcessorType = ExportProcessorType.Batch;
        options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
        {
            MaxQueueSize = 2048,
            ScheduledDelayMilliseconds = 5000,
            ExporterTimeoutMilliseconds = 30000,
            MaxExportBatchSize = 512
        };
    }

    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("Application", AppName)
                .Enrich.WithProperty("Version", Version)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = OtelCollectorEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = AppName,
                        ["service.version"] = Version,
                        ["service.namespace"] = "Identity",
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    };
                })
                .WriteTo.Console(outputTemplate: 
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });
    }
}

public class CustomMetrics
    {
        private readonly Counter<long> _userRegistrationCounter;
        private readonly Histogram<double> _authenticationDuration;
        private readonly UpDownCounter<long> _activeUserSessions;
        private readonly Counter<long> _failedLoginAttempts;
        private readonly ObservableGauge<long> _totalRegisteredUsers;

        private long _registeredUsersCount = 0;

        public CustomMetrics(Meter meter)
        {
            _userRegistrationCounter = meter.CreateCounter<long>(
                "identity.user.registrations",
                unit: "{registrations}",
                description: "Number of user registrations");

            _authenticationDuration = meter.CreateHistogram<double>(
                "identity.authentication.duration",
                unit: "ms",
                description: "Authentication request duration");

            _activeUserSessions = meter.CreateUpDownCounter<long>(
                "identity.active_sessions",
                unit: "{sessions}",
                description: "Number of active user sessions");

            _failedLoginAttempts = meter.CreateCounter<long>(
                "identity.login.failures",
                unit: "{attempts}",
                description: "Number of failed login attempts");

            _totalRegisteredUsers = meter.CreateObservableGauge<long>(
                "identity.users.total",
                () => _registeredUsersCount,
                unit: "{users}",
                description: "Total number of registered users");
        }

        public void RecordRegistration()
        {
            _userRegistrationCounter.Add(1);
            Interlocked.Increment(ref _registeredUsersCount);
        }

        public void RecordAuthenticationDuration(double milliseconds) =>
            _authenticationDuration.Record(milliseconds);

        public void IncrementActiveSessions() =>
            _activeUserSessions.Add(1);

        public void DecrementActiveSessions() =>
            _activeUserSessions.Add(-1);

        public void RecordFailedLoginAttempt(string reason) =>
            _failedLoginAttempts.Add(1, new KeyValuePair<string, object>("reason", reason));
    }