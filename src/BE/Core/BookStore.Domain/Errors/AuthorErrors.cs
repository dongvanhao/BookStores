using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class AuthorErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound("Author.NotFound", $"Author '{id}' was not found.");

    public static readonly Error FullNameExists
        = Error.Conflict("Author.FullNameExists", "An author with this name already exists.");

    public static readonly Error HasBooks
        = Error.Conflict("Author.HasBooks", "Cannot delete author because they are linked to one or more books.");

    public static readonly Error AvatarTooLarge
        = Error.Validation("Author.AvatarTooLarge", "Avatar file size must not exceed 5 MB.");

    public static readonly Error AvatarInvalidFormat
        = Error.Validation("Author.AvatarInvalidFormat", "Avatar must be a .jpg, .jpeg, .png, or .webp file.");
}
