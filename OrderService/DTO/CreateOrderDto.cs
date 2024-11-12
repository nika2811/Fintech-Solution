using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public class CreateOrderDto
{
    [Required] public Guid CompanyId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(10, ErrorMessage = "Currency code cannot exceed 10 characters.")]
    public string Currency { get; set; }
}