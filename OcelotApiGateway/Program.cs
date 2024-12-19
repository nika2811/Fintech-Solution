using Consul;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Point to ocelot.json file directly
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Expose on port 5000
});

// Add services to the container
builder.Services.AddOcelot(builder.Configuration)
    .AddConsul() // Enable Consul integration
    .AddPolly();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ocelot API Gateway",
        Version = "v1",
        Description = "API Gateway for routing requests using Ocelot"
    });
});
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IConsulClient, ConsulClient>(provider => new ConsulClient(config =>
{
    config.Address = new Uri("http://localhost:8500");
}));

// Build the app first
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ocelot API Gateway v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.UseOcelot().Wait();
 
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();