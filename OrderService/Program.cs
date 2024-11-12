using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Middleware;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Services.Auth;
using OrderService.StartupExtensions;
using OrderService.StartupExtensions.Logging;
using OrderService.StartupExtensions.MassTransit;
using OrderService.StartupExtensions.RateLimiter;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddCentralizedObservability();

builder.Services.AddCustomServices(builder.Configuration);

builder.Services.ConfigureRateLimiter(builder.Configuration);

builder.Services.AddMassTransitServices(builder.Configuration);

// builder.Services.AddDbContext<OrderDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//
// builder.Services.AddScoped<IOrderRepository, OrderRepository>();
// builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
// builder.Services.AddScoped<IAuthService, AuthService>();

// builder.Services.AddMassTransit(busConfigurator =>
// {
//     busConfigurator.SetKebabCaseEndpointNameFormatter();
//     busConfigurator.AddConsumer<PaymentProcessedEventConsumer>();
//     busConfigurator.UsingRabbitMq((context, configurator) =>
//     {
//         configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), h =>
//         {
//             h.Username(builder.Configuration["MessageBroker:Username"]);
//             h.Password(builder.Configuration["MessageBroker:Password"]);
//         });
//         configurator.ReceiveEndpoint("order-service-queue",
//             e => { e.ConfigureConsumer<PaymentProcessedEventConsumer>(context); });
//         configurator.ConfigureEndpoints(context);
//     });
// });


// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
//
//     // Add API key and secret as global headers in Swagger
//     c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
//     {
//         Description = "API Key needed to access the endpoints. X-Api-Key: Your_API_Key",
//         In = ParameterLocation.Header,
//         Name = "X-Api-Key",
//         Type = SecuritySchemeType.ApiKey
//     });
//
//     c.AddSecurityDefinition("ApiSecret", new OpenApiSecurityScheme
//     {
//         Description = "API Secret needed to access the endpoints. X-Api-Secret: Your_API_Secret",
//         In = ParameterLocation.Header,
//         Name = "X-Api-Secret",
//         Type = SecuritySchemeType.ApiKey
//     });
//
//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
//             },
//             new List<string>()
//         },
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiSecret" }
//             },
//             new List<string>()
//         }
//     });
// });

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

app.MapControllers();

app.Run();