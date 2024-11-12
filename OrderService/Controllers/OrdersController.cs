using Microsoft.AspNetCore.Mvc;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services;
using OrderService.Services.Auth;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderService orderService, IAuthService authService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromHeader] string apiKey, [FromHeader] string apiSecret,
        [FromBody] CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            return Unauthorized(new { error = "Missing API Key or Secret" });
        }

        if (!await authService.ValidateRequestAsync(apiKey, apiSecret, dto))
        {
            return Unauthorized(new { error = "Invalid API Key or Secret" });
        }

        var order = await orderService.CreateOrderAsync(dto.CompanyId, dto.Amount, dto.Currency);

        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult> GetOrderById(Guid orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound(new { error = "Order not found" });
        }
        return Ok(order);
    }

    [HttpGet("company/{companyId:guid}")]
    public async Task<IActionResult> GetOrdersByCompanyId(Guid companyId)
    {
        var orders = await orderService.GetOrdersByCompanyIdAsync(companyId);
        
        if (!orders.Any())
        {
            return NotFound(new { error = "No orders found for the specified company" });
        }
        
        return Ok(orders);
    }

    [HttpGet("compute/{companyId:guid}")]
    public async Task<IActionResult> ComputeTotalOrders(Guid companyId)
    {
        var total = await orderService.ComputeTotalOrdersAsync(companyId);
        return Ok(new { total });
    }

    [HttpGet("{orderId:guid}/exists")]
    public async Task<IActionResult> OrderExists(Guid orderId, [FromQuery] Guid companyId)
    {
        if (orderId == Guid.Empty || companyId == Guid.Empty)
            return BadRequest(new { error = "OrderId or CompanyId is missing or invalid." });

        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.CompanyId != companyId) return Ok(new { exists = false });

        return Ok(new { exists = true });
    }
}