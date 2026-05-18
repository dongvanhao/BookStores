using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class BookErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Book.NotFound", $"Book '{id}' was not found.");

    public static Error ISBNExists(string isbn)
        => Error.Conflict("Book.ISBNExists", $"A book with ISBN '{isbn}' already exists.");

    public static readonly Error TitleExists
        = Error.Conflict("Book.TitleExists", "A book with this title already exists.");

    public static readonly Error InvalidCoverFile
        = Error.Validation("Book.InvalidCoverFile", "Cover must be an image file under 5 MB.");

    public static Error CategoryNotFound(Guid id)
        => Error.NotFound("Book.CategoryNotFound", $"Category '{id}' was not found.");

    public static Error InsufficientStock(Guid id)
        => Error.Validation("Book.InsufficientStock", $"Book '{id}' does not have sufficient stock.");
}
