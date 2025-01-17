using PaymentProcessorService.Endpoints;
using PaymentProcessorService.Middleware;
using PaymentProcessorService.StartupExtensions;
using PaymentProcessorService.StartupExtensions.Consul;
using PaymentProcessorService.StartupExtensions.MassTransit;
using PaymentProcessorService.StartupExtensions.Observability;
using PaymentProcessorService.StartupExtensions.RateLimiter;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.AddCentralizedObservability();

builder.Services.AddCustomServices(builder.Configuration);

builder.Services.ConfigureRateLimiter(builder.Configuration);

builder.Services.AddMassTransitServices(builder.Configuration);

builder.Services.AddConsulServiceDiscovery(builder.Configuration);


builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await DatabaseMigration.MigrateDatabaseAsync(app.Services);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();

app.UseCors("DefaultCorsPolicy");

app.UseRouting();

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapPaymentEndpoints();
app.MapMetrics();

app.Run();