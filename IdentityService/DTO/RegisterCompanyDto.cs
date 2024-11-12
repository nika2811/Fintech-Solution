using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTO;

public class RegisterCompanyDto
{
    [Required]
    public string Name { get; set; }
}