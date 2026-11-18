using FluentValidation;
using MedFind.Application.DTOs;

namespace MedFind.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<AuthRegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Role).Must(r => r == "Patient" || r == "PharmacyAdmin")
            .WithMessage("Role must be 'Patient' or 'PharmacyAdmin'.");
    }
}

public class LoginRequestValidator : AbstractValidator<AuthLoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
