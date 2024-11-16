using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
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

namespace PaymentProcessorService.StartupExtensions.Observability;

public static class ObservabilityConfiguration
{
    private const string AppName = "paymentProcessorService";
    private const string Version = "1.0.0";
    private const string ServiceNamespace = "PaymentProcessor";
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
            options.AddOtlpExporter(ConfigureOtlpExporter);
        });
    }

    private static ResourceBuilder CreateResourceBuilder(WebApplicationBuilder builder)
    {
        var instanceId = Guid.NewGuid().ToString();

        return ResourceBuilder.CreateDefault()
            .AddService(AppName, serviceVersion: Version, serviceInstanceId: Environment.MachineName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector()
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["host.name"] = Environment.MachineName,
                ["process.runtime.name"] = RuntimeInformation.FrameworkDescription,
                ["service.namespace"] = ServiceNamespace,
                ["service.instance.id"] = instanceId,
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
                options.EnrichWithHttpRequest = EnrichHttpRequestWithCorrelationId;
                options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequestMessage = EnrichHttpClientWithCorrelationId;
                options.FilterHttpRequestMessage = msg => !msg.RequestUri!.PathAndQuery.StartsWith("/health");
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.EnrichWithIDbCommand = EnrichWithDbParameters;
            })
            .AddSqlClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.SetDbStatementForText = true;
                options.EnableConnectionLevelAttributes = true;
            })
            .AddGrpcClientInstrumentation()
            .AddOtlpExporter(ConfigureOtlpExporter);
    }

    private static void EnrichHttpRequestWithCorrelationId(Activity activity, HttpRequest request)
    {
        if (request.Headers.TryGetValue("x-correlation-id", out var correlationId))
            activity.SetTag("custom.http.request.header.x-correlation-id", correlationId.ToString());
    }

    private static void EnrichHttpClientWithCorrelationId(Activity activity, HttpRequestMessage request)
    {
        if (request.Headers.Contains("x-correlation-id"))
            activity.SetTag("custom.http.request.header.x-correlation-id",
                request.Headers.GetValues("x-correlation-id").FirstOrDefault());
    }

    private static void EnrichWithDbParameters(Activity activity, IDbCommand command)
    {
        var parameters = command.Parameters.Cast<DbParameter>()
            .Select(p => $"{p.ParameterName}={p.Value}")
            .ToArray();

        if (parameters.Length > 0) activity.SetTag("db.statement.parameters", string.Join(", ", parameters));
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics)
    {
        metrics
            .AddMeter(AppName)
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEventCountersInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(ConfigureOtlpExporter);
    }

    private static void ConfigureOtlpExporter(OtlpExporterOptions options)
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
                        ["service.namespace"] = ServiceNamespace,
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    };
                })
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });
    }
}