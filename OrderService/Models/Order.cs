namespace OrderService.Models;

public class Order(Guid companyId, decimal amount, string currency)
{
    public Guid OrderId { get; private set; } = Guid.NewGuid();
    public Guid CompanyId { get; private set; } = companyId;
    public decimal Amount { get; private set; } = amount;
    public string Currency { get; private set; } = currency;
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;


    public void MarkAsCompleted()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only orders in Created status can be marked as Completed.");

        Status = OrderStatus.Completed;
    }

    public void MarkAsRejected()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only orders in Created status can be marked as Rejected.");

        Status = OrderStatus.Rejected;
    }
}