namespace OrderService.Services.Auth;

public class IdentityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}