using BookStore.Application.Categories.Commands;
using BookStore.Shared.Results;

namespace BookStore.Application.Categories.IService;

public interface ICategoryCommandService
{
    Task<Result<Guid>> CreateAsync(CreateCategoryCommand cmd, CancellationToken ct = default);
    Task<Result>       UpdateAsync(Guid id, UpdateCategoryCommand cmd, CancellationToken ct = default);
    Task<Result>       PatchAsync(Guid id, PatchCategoryCommand cmd, CancellationToken ct = default);
    Task<Result>       DeleteAsync(Guid id, CancellationToken ct = default);
}
