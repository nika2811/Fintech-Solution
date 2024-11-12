using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTO;

public class ValidateCredentialsRequest
{
    [Required] [MaxLength(64)] public string ApiKey { get; set; }

    [Required] [MaxLength(64)] public string ApiSecret { get; set; }
}