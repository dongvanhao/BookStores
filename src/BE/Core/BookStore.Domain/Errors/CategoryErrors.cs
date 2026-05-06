using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class CategoryErrors
{
    public static readonly Error SelfParent
        = Error.Validation("Category.SelfParent", "A category cannot be its own parent.");

    public static readonly Error CircularReference
        = Error.Validation("Category.CircularReference", "Setting this parent would create a circular reference.");

    public static readonly Error HasChildren
        = Error.Conflict("Category.HasChildren", "Cannot delete a category that has child categories.");

    public static readonly Error HasBooks
        = Error.Conflict("Category.HasBooks", "Cannot delete a category that has associated books.");

    public static Error NotFound(Guid id)
        => Error.NotFound("Category.NotFound", $"Category '{id}' was not found.");

    public static Error ParentNotFound(Guid id)
        => Error.NotFound("Category.ParentNotFound", $"Parent category '{id}' was not found.");
}
