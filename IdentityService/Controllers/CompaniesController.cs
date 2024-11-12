using System.Diagnostics;
using IdentityService.DTO;
using IdentityService.Extension;
using IdentityService.Services;
using IdentityService.StartupExtensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController(ICompanyService companyService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyDto dto)
    { 
        var company = await companyService.RegisterCompanyAsync(dto.Name);
        return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCompanyById(Guid id)
    {
        if (id == Guid.Empty) return BadRequest(new { error = "Invalid company ID." });

        var company = await companyService.GetCompanyByIdAsync(id);
        if (company == null) return NotFound(new { error = "Company not found." });
        return Ok(company);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateApiKeyAndSecret([FromBody] ValidateCredentialsRequest request)
    {
        var company = await companyService.GetCompanyByApiKeyAsync(request.ApiKey);

        if (company == null || StringSecureEquals.SecureEquals(company.ApiSecret, request.ApiSecret) == false)
            return Unauthorized(new { error = "Invalid API key or secret." });

        return Ok(new { CompanyId = company.Id });
    }
}