namespace BookStore.Application.Books.Commands;

public record UpdateBookCommand(
    string Title,
    string? Description,
    string ISBN,
    int PublishedYear,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    IReadOnlyList<Guid> AuthorIds
);
