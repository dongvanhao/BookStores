using BookStore.Application.Authors.DTOs;
using BookStore.Application.Authors.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Results;

namespace BookStore.Application.Authors.IService;

public interface IAuthorQueryService
{
    Task<Result<PagedResult<AuthorDto>>> GetPagedAsync(GetAuthorsQuery query, CancellationToken ct = default);
    Task<Result<AuthorDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}
