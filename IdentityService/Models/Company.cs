using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace IdentityService.Models;

public class Company(string name)
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required] [MaxLength(100)] public string Name { get; private set; } = name;

    [Required] [MaxLength(64)] public string ApiKey { get; private set; } = GenerateApiKey();

    [Required] public string ApiSecret { get; private set; } = GenerateApiSecret();

    private static string GenerateApiKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string GenerateApiSecret()
    {
        using var hmac = new HMACSHA256();
        var secret = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(hmac.ComputeHash(secret));
    }
}