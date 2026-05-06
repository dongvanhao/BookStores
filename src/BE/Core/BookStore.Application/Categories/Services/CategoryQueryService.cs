using BookStore.Application.Categories.DTOs;
using BookStore.Application.Categories.IService;
using BookStore.Application.Categories.Queries;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Common;
using BookStore.Shared.Extensions;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Categories.Services;

public class CategoryQueryService(ICategoryRepository categoryRepo) : ICategoryQueryService
{
    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return CategoryErrors.NotFound(id);

        return ToDto(category);
    }

    public async Task<Result<PagedResult<CategoryDto>>> GetPagedAsync(
        GetCategoriesQuery query, CancellationToken ct = default)
    {
        var queryable = categoryRepo.GetQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            queryable = queryable.Where(c => c.Name.Contains(query.SearchTerm));

        if (query.ParentId.HasValue)
            queryable = queryable.Where(c => c.ParentId == query.ParentId);

        var result = await queryable
            .ApplySort(query.SortBy, query.IsAscending)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Description,
                c.ParentId,
                c.Parent != null ? c.Parent.Name : null,
                c.Children.Count,
                c.CreatedAt,
                c.UpdatedAt))
            .ToPagedResultAsync(query, ct);

        return result;
    }

    public async Task<Result<List<CategoryTreeDto>>> GetTreeAsync(CancellationToken ct = default)
    {
        var all = await categoryRepo.GetAllWithChildrenAsync(ct);
        var roots = all.Where(c => c.ParentId == null).ToList();
        return roots.Select(BuildTreeDto).ToList();
    }

    public async Task<Result<CategoryTreeDto>> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var all = await categoryRepo.GetAllWithChildrenAsync(ct);
        var root = all.FirstOrDefault(c => c.Id == id);
        if (root is null)
            return CategoryErrors.NotFound(id);

        return BuildTreeDto(root);
    }

    private static CategoryDto ToDto(Category c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.ParentId,
        c.Parent?.Name,
        c.Children.Count,
        c.CreatedAt,
        c.UpdatedAt
    );

    private static CategoryTreeDto BuildTreeDto(Category c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.Children.Select(BuildTreeDto).ToList()
    );
}
