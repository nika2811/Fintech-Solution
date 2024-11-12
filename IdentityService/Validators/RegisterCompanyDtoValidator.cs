using FluentValidation;
using IdentityService.DTO;

namespace IdentityService.Validators;

public class RegisterCompanyDtoValidator : AbstractValidator<RegisterCompanyDto>
{
    public RegisterCompanyDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");
    }
}