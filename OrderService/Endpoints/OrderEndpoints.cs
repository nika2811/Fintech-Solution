using Microsoft.AspNetCore.Mvc;
using OrderService.DTO;
using OrderService.Services;
using OrderService.Services.Auth;

namespace OrderService.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders");

        orders.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithOpenApi();

        orders.MapGet("/{id:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .WithOpenApi();

        orders.MapGet("/company/{companyId:guid}", GetOrdersByCompanyIdAsync)
            .WithName("GetOrdersByCompanyId")
            .WithOpenApi();

        orders.MapGet("/compute/{companyId:guid}", ComputeTotalOrdersAsync)
            .WithName("ComputeTotalOrders")
            .WithOpenApi();

        orders.MapGet("/{orderId:guid}/exists", CheckOrderExistsAsync)
            .WithName("CheckOrderExists")
            .WithOpenApi();
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromHeader(Name = "X-API-Key")] string apiKey,
        [FromHeader(Name = "X-API-Secret")] string apiSecret,
        CreateOrderDto dto,
        IOrderService orderService,
        IAuthService authService,
        LinkGenerator linkGenerator)
    {
        // Validate API credentials
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            return Results.Unauthorized();

        if (!await authService.ValidateRequestAsync(apiKey, apiSecret, dto))
            return Results.Unauthorized();

        // Create order
        var order = await orderService.CreateOrderAsync(dto.CompanyId, dto.Amount, dto.Currency);
    
        // Use LinkGenerator to create the location dynamically
        var location = linkGenerator.GetPathByName("GetOrderById", new { id = order.OrderId });
    
        return Results.Created(location, order);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid id, 
        IOrderService orderService)
    {
        if (id == Guid.Empty) 
            return Results.BadRequest(new { error = "Invalid order ID." });

        var order = await orderService.GetOrderByIdAsync(id);
        
        return order is null 
            ? Results.NotFound(new { error = "Order not found." }) 
            : Results.Ok(order);
    }

    private static async Task<IResult> GetOrdersByCompanyIdAsync(
        Guid companyId, 
        IOrderService orderService)
    {
        if (companyId == Guid.Empty) 
            return Results.BadRequest(new { error = "Invalid company ID." });

        var orders = await orderService.GetOrdersByCompanyIdAsync(companyId);
        
        return orders.Any() 
            ? Results.Ok(orders) 
            : Results.NotFound(new { error = "No orders found for the specified company" });
    }

    private static async Task<IResult> ComputeTotalOrdersAsync(
        Guid companyId, 
        IOrderService orderService)
    {
        if (companyId == Guid.Empty) 
            return Results.BadRequest(new { error = "Invalid company ID." });

        var total = await orderService.ComputeTotalOrdersAsync(companyId);
        
        return Results.Ok(new { total });
    }

    private static async Task<IResult> CheckOrderExistsAsync(
        Guid orderId, 
        [FromQuery] Guid companyId, 
        IOrderService orderService)
    {
        if (orderId == Guid.Empty || companyId == Guid.Empty)
            return Results.BadRequest(new { error = "OrderId or CompanyId is missing or invalid." });

        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.CompanyId != companyId) return Results.Ok(new { exists = false });
        return Results.Ok(new { exists = true });
    }
    
}