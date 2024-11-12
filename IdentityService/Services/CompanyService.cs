using IdentityService.Models;
using IdentityService.Repositories;

namespace IdentityService.Services;

public class CompanyService(ICompanyRepository companyRepository, ILogger<CompanyService> logger) : ICompanyService
{
    public async Task<Company> RegisterCompanyAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Attempted to register a company with an invalid name.");
            throw new ArgumentException("Company name cannot be null or empty.", nameof(name));
        }

        var existingCompany = await companyRepository.GetCompanyByNameAsync(name, cancellationToken);
        if (existingCompany != null)
        {
            logger.LogWarning("Company with name {CompanyName} already exists.", name);
            throw new InvalidOperationException($"A company with the name '{name}' already exists.");
        }

        var company = new Company(name);
        try
        {
            var addedCompany = await companyRepository.AddCompanyAsync(company, cancellationToken);
            logger.LogInformation("Company {CompanyId} registered successfully.", addedCompany.Id);
            return addedCompany;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while registering company with name: {CompanyName}", name);
            throw;
        }
    }

    public async Task<Company> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving company with ID: {CompanyId}", companyId);
        return await companyRepository.GetCompanyByIdAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetAllCompaniesAsync(int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            logger.LogWarning("Invalid page number: {Page}. Page number must be greater than 0.", page);
            throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");
        }

        if (pageSize < 1)
        {
            logger.LogWarning("Invalid page size: {PageSize}. Page size must be greater than 0.", pageSize);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
        }

        logger.LogInformation("Retrieving companies - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        return await companyRepository.GetAllCompaniesAsync(page, pageSize, cancellationToken);
    }

    public async Task<Company> GetCompanyByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Attempted to retrieve a company with an invalid API key.");
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        }

        logger.LogInformation("Retrieving company with API key: {ApiKey}", apiKey);
        return await companyRepository.GetCompanyByApiKeyAsync(apiKey, cancellationToken);
    }
}