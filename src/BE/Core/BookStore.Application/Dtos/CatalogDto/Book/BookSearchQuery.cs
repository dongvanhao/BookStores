namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public record BookSearchQuery(
        string? Keyword,
        Guid? AuthorId,
        Guid? CategoryId,
        int Page = 1,
        int PageSize = 20
    );
}
