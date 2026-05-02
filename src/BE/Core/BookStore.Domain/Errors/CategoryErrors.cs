using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class CategoryErrors
{
    public static readonly Error SelfParent
        = Error.Validation("Category.SelfParent", "A category cannot be its own parent.");

    public static Error NotFound(Guid id)
        => Error.NotFound("Category.NotFound", $"Category '{id}' was not found.");
}
