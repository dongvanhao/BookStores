using BookStore.Application.Books.Commands;
using FluentValidation;

namespace BookStore.API.Validators.Books;

public class PatchBookCommandValidator : AbstractValidator<PatchBookCommand>
{
    public PatchBookCommandValidator()
    {
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title!)
                .NotEmpty().WithMessage("Title must not be empty.")
                .MaximumLength(300).WithMessage("Title must not exceed 300 characters."));

        When(x => x.ISBN is not null, () =>
            RuleFor(x => x.ISBN!)
                .NotEmpty().WithMessage("ISBN must not be empty.")
                .MaximumLength(20).WithMessage("ISBN must not exceed 20 characters."));

        When(x => x.PublishedYear.HasValue, () =>
            RuleFor(x => x.PublishedYear!.Value)
                .InclusiveBetween(1000, DateTime.UtcNow.Year)
                .WithMessage($"Published year must be between 1000 and {DateTime.UtcNow.Year}."));

        When(x => x.Price.HasValue, () =>
            RuleFor(x => x.Price!.Value)
                .GreaterThan(0).WithMessage("Price must be greater than zero."));

        When(x => x.StockQuantity.HasValue, () =>
            RuleFor(x => x.StockQuantity!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative."));

        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!)
                .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters."));
    }
}
