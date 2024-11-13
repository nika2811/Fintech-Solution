using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Repositories;

public class CompanyRepository(IdentityDbContext context) : ICompanyRepository
{
    public async Task<Company> AddCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        await context.Companies.AddAsync(company, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return company;
    }

    public async Task<Company?> GetCompanyByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be null or whitespace.", nameof(name));

        return await context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }


    public async Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Companies
            .AsNoTracking()
            .TagWith("GetCompanyById")
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetCompanyByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await context.Companies.SingleOrDefaultAsync(c => c.ApiKey == apiKey, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetAllCompaniesAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await context.Companies
            .AsNoTracking()
            .TagWith("GetAllCompanies")
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}