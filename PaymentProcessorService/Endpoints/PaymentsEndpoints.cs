using Microsoft.AspNetCore.Mvc;
using PaymentProcessorService.DTO;
using PaymentProcessorService.Services;
using PaymentProcessorService.Services.Auth;
using Shared.Events.Payment;

namespace PaymentProcessorService.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var payments = app.MapGroup("/api/payments");

        payments.MapPost("/", ProcessPaymentAsync)
            .WithName("ProcessPayment")
            .WithOpenApi();

        payments.MapGet("/{paymentId:guid}", GetPaymentByIdAsync)
            .WithName("GetPaymentById")
            .WithOpenApi();

        payments.MapGet("/", GetAllPaymentsAsync)
            .WithName("GetAllPayments")
            .WithOpenApi();
    }

    private static async Task<IResult> ProcessPaymentAsync(
        [FromHeader] string apiKey,
        [FromHeader] string apiSecret,
        [FromBody] ProcessPaymentDto dto,
        IPaymentService paymentService,
        IAuthService authService,
        LinkGenerator linkGenerator)
    {
        // Validate API Key and Secret
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return Results.Problem("Missing API Key or Secret", statusCode: StatusCodes.Status401Unauthorized);

        var validationResult = await authService.ValidateRequestAsync(apiKey, apiSecret);

        if (!validationResult.isValid)
            return Results.Problem("Invalid API Key or Secret", statusCode: StatusCodes.Status401Unauthorized);

        // Process Payment
        var payment = await paymentService.ProcessPaymentAsync(
            dto.OrderId,
            dto.CardNumber,
            dto.ExpiryDate,
            validationResult.companyId
        );

        if (payment.Status == PaymentStatus.Rejected)
            return Results.BadRequest(new ErrorResponse("Payment was rejected."));

        // Create response with payment details
        var paymentResponse = new
        {
            payment.PaymentId,
            payment.OrderId,
            payment.Status,
            payment.CreatedAt
        };

        // Generate location header using link generator
        var location = linkGenerator.GetPathByName("GetPaymentById", new { paymentId = payment.PaymentId });

        return Results.Created(location, paymentResponse);
    }

    private static async Task<IResult> GetPaymentByIdAsync(
        Guid paymentId,
        IPaymentService paymentService)
    {
        // Validate GUID
        if (paymentId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("Invalid Payment ID"));

        var payment = await paymentService.GetPaymentByIdAsync(paymentId);

        return payment != null
            ? Results.Ok(payment)
            : Results.NotFound(new ErrorResponse($"Payment with ID {paymentId} not found."));
    }

    private static async Task<IResult> GetAllPaymentsAsync(
        IPaymentService paymentService)
    {
        var payments = await paymentService.GetAllPaymentsAsync();
        return Results.Ok(payments);
    }

    // Centralized error response record
    private record ErrorResponse(string Message);
}