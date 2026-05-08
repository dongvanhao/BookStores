using BookStore.Application.Categories.Commands;
using FluentValidation;

namespace BookStore.API.Validators;

public class PatchCategoryCommandValidator : AbstractValidator<PatchCategoryCommand>
{
    public PatchCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name must not be empty.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);
    }
}
