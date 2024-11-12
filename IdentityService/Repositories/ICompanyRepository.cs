using IdentityService.Models;

namespace IdentityService.Repositories;

public interface ICompanyRepository
{
    Task<Company> AddCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task<Company?> GetCompanyByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllCompaniesAsync(int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}