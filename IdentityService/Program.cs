using IdentityService.Endpoints;
using IdentityService.Middleware;
using IdentityService.StartupExtensions;
using IdentityService.StartupExtensions.Consul;
using IdentityService.StartupExtensions.MassTransit;
using IdentityService.StartupExtensions.Observability;
using IdentityService.StartupExtensions.RateLimiter;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedObservability();

builder.Services.AddControllers();

builder.Services.AddCustomServices(builder.Configuration);

builder.Services.ConfigureRateLimiter(builder.Configuration);

builder.Services.AddMassTransitServices(builder.Configuration);

builder.Services.AddConsulServiceDiscovery(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

await DatabaseMigration.MigrateDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapCompaniesEndpoints();
app.MapMetrics();

app.UseRateLimiter();

app.Run();