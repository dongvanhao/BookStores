namespace BookStore.Application.Books.Commands;

public record CreateBookCommand(
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    IReadOnlyList<Guid> AuthorIds
);
