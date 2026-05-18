using BookStore.Application.Books.DTOs;
using BookStore.Application.Books.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Results;

namespace BookStore.Application.Books.IService;

public interface IBookQueryService
{
    Task<Result<BookDetailDto>>        GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<BookDto>>> GetPagedAsync(GetBooksQuery query, CancellationToken ct = default);
}
