using BookStore.Application.Auth.Commands;
using FluentValidation;

namespace BookStore.API.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
    RuleFor(x => x.Email)
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Email is required.")
        .EmailAddress().WithMessage("Invalid email format.")
        .MaximumLength(254).WithMessage("Email must not exceed 254 characters.");

    RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password is required.")
        .MaximumLength(100);
    }
}
