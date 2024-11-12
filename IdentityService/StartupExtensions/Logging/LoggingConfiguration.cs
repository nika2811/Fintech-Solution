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

namespace IdentityService.StartupExtensions.Logging;

public static class LoggingConfiguration
{
    private const string AppName = "IdentityService";
    private const string Version = "1.0.0";

    // Single point of configuration for OpenTelemetry collector
    private static readonly string OtelCollectorEndpoint = Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT") 
        ?? "http://otel-collector:4317";

    public static void AddCentralizedObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var activitySource = new ActivitySource(AppName, Version);
        var meter = new Meter(AppName, Version);

        builder.Services.AddSingleton(activitySource);
        builder.Services.AddSingleton(meter);
        
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
