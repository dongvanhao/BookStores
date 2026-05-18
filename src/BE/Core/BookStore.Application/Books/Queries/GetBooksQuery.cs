using BookStore.Shared.Common;

namespace BookStore.Application.Books.Queries;

public sealed class GetBooksQuery : QueryParams
{
    public Guid?    CategoryId { get; set; }
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
}
