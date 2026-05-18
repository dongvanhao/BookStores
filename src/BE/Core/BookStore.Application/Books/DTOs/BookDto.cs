namespace BookStore.Application.Books.DTOs;

public record BookDto(
    Guid Id,
    string Title,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    string? CoverUrl,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<string> AuthorNames,
    DateTime CreatedAt
);
