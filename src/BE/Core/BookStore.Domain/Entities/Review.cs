using BookStore.Domain.Common;
using BookStore.Domain.Errors;
using BookStore.Shared.Results;

namespace BookStore.Domain.Entities;

public class Review : BaseEntity
{
    public int Rating { get; private set; }       // 1–5
    public string? Comment { get; private set; }

    // FK
    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;

    public Guid BookId { get; private set; }
    public Book Book { get; private set; } = null!;

    private Review() { }

    public static Result<Review> Create(Guid userId, Guid bookId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            return ReviewErrors.InvalidRating;

        return new Review
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            BookId    = bookId,
            Rating    = rating,
            Comment   = comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public Result Update(int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            return Result.Failure(ReviewErrors.InvalidRating);

        Rating    = rating;
        Comment   = comment;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
