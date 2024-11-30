using IdentityService.DTO;
using IdentityService.Extension;
using IdentityService.Services;

namespace IdentityService.Endpoints;

public static class CompaniesEndpoints
{
    public static void MapCompaniesEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .WithOpenApi();

        companies.MapPost("/", RegisterCompanyAsync)
            .WithName("RegisterCompany")
            .WithOpenApi();

        companies.MapGet("/{id:guid}", GetCompanyByIdAsync)
            .WithName("GetCompanyById")
            .WithOpenApi();

        companies.MapPost("/validate", ValidateCredentialsAsync)
            .WithName("ValidateApiKeyAndSecret")
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterCompanyAsync(RegisterCompanyDto dto, ICompanyService companyService)
    {
        var company = await companyService.RegisterCompanyAsync(dto.Name);
        return Results.Created($"/api/companies/{company.Id}", company);
    }

    private static async Task<IResult> GetCompanyByIdAsync(Guid id, ICompanyService companyService)
    {
        if (id == Guid.Empty)
            return Results.BadRequest(new { error = "Invalid company ID." });

        var company = await companyService.GetCompanyByIdAsync(id);

        return company is null
            ? Results.NotFound(new { error = "Company not found." })
            : Results.Ok(company);
    }

    private static async Task<IResult> ValidateCredentialsAsync(ValidateCredentialsRequest request,
        ICompanyService companyService)
    {
        var company = await companyService.GetCompanyByApiKeyAsync(request.ApiKey);

        if (company is null || !StringSecureEquals.SecureEquals(company.ApiSecret, request.ApiSecret))
            return Results.Unauthorized();

        return Results.Ok(new { CompanyId = company.Id });
    }
}