using BookStore.Application.Categories.DTOs;
using BookStore.Application.Categories.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Results;

namespace BookStore.Application.Categories.IService;

public interface ICategoryQueryService
{
    Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResult<CategoryDto>>> GetPagedAsync(GetCategoriesQuery query, CancellationToken ct = default);
    Task<Result<List<CategoryTreeDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<CategoryTreeDto>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
}
