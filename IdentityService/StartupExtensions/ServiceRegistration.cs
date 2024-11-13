using FluentValidation;
using FluentValidation.AspNetCore;
using IdentityService.Data;
using IdentityService.Repositories;
using IdentityService.Services;
using IdentityService.Validators;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.StartupExtensions;

public static class ServiceRegistration
{
    public static void AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // Enable retry on failure for transient fault handling
                    npgsqlOptions.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null);
                }));

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyService, CompanyService>();

        services.AddValidatorsFromAssemblyContaining<RegisterCompanyDtoValidator>();
        services.AddFluentValidationAutoValidation();
    }
}