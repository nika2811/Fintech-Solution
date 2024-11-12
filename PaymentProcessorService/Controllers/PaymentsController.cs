using Microsoft.AspNetCore.Mvc;
using PaymentProcessorService.DTO;
using PaymentProcessorService.Services;
using PaymentProcessorService.Services.Auth;
using Shared.Events.Payment;

namespace PaymentProcessorService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(IPaymentService paymentService, IAuthService authService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromHeader] string apiKey, [FromHeader] string apiSecret,
        [FromBody] ProcessPaymentDto dto)
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return Unauthorized("Missing API Key or Secret");

        var validationResult  = await authService.ValidateRequestAsync(apiKey, apiSecret);

        if (validationResult.isValid == false) return Unauthorized("Invalid API Key or Secret");

        var payment =
            await paymentService.ProcessPaymentAsync(dto.OrderId, dto.CardNumber, dto.ExpiryDate, validationResult .companyId);
        
        if (payment.Status == PaymentStatus.Rejected)
            return BadRequest("Payment was rejected.");

        return Ok(new { payment.PaymentId, payment.OrderId, payment.Status, payment.CreatedAt });
    }


    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var payment = await paymentService.GetPaymentByIdAsync(paymentId);
        return payment != null
            ? Ok(payment)
            : NotFound(new { Message = $"Payment with ID {paymentId} not found." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPayments()
    {
        var payments = await paymentService.GetAllPaymentsAsync();
        return Ok(payments);
    }
}