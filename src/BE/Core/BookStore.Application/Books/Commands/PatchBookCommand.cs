namespace BookStore.Application.Books.Commands;

public record PatchBookCommand(
    string? Title,
    string? Description,
    string? ISBN,
    int? PublishedYear,
    decimal? Price,
    int? StockQuantity,
    Guid? CategoryId,
    IReadOnlyList<Guid>? AuthorIds
);
