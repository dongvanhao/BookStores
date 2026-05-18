using BookStore.Application.Authors.Commands;
using FluentValidation;

namespace BookStore.API.Validators.Authors;

public class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(150).WithMessage("FullName must not exceed 150 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => x.Bio is not null);
    }
}
