namespace PaymentProcessorService.DTO;

public class ProcessPaymentDto
{
    public Guid OrderId { get; set; }
    public string CardNumber { get; set; }
    public DateTime ExpiryDate { get; set; }
}