using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Endpoints;
using OrderService.Middleware;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Services.Auth;
using OrderService.StartupExtensions;
using OrderService.StartupExtensions.MassTransit;
using OrderService.StartupExtensions.Observability;
using OrderService.StartupExtensions.RateLimiter;
using Prometheus;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedObservability();

builder.Services.AddCustomServices(builder.Configuration);

builder.Services.ConfigureRateLimiter(builder.Configuration);

builder.Services.AddMassTransitServices(builder.Configuration);

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

builder.Services.AddMemoryCache();

var app = builder.Build();

await DatabaseMigration.MigrateDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCorsPolicy");

app.UseRouting();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapOrderEndpoints();
app.MapMetrics();

app.Run();