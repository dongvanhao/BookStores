namespace BookStore.Application.Books.DTOs;

public record BookDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    string? CoverUrl,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<AuthorSummaryDto> Authors,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
