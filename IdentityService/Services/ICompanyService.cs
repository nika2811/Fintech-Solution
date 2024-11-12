using IdentityService.Models;

namespace IdentityService.Services;

public interface ICompanyService
{
    Task<Company> RegisterCompanyAsync(string name, CancellationToken cancellationToken = default);
    Task<Company> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllCompaniesAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<Company> GetCompanyByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}