using BookStore.Shared.Results;

namespace BookStore.Domain.Errors;

public static class ReviewErrors
{
    public static readonly Error InvalidRating
        = Error.Validation("Review.InvalidRating", "Rating must be between 1 and 5.");

    public static Error NotFound(Guid id)
        => Error.NotFound("Review.NotFound", $"Review '{id}' was not found.");

    public static readonly Error AlreadyReviewed
        = Error.Conflict("Review.AlreadyReviewed", "User has already reviewed this book.");
}
