using BookStore.Application.Categories.DTOs;
using BookStore.Application.Categories.IService;
using BookStore.Application.Categories.Queries;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Common;
using BookStore.Shared.Extensions;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Categories.Services;

public class CategoryQueryService(
    ICategoryRepository categoryRepo,
    IMinioStorageService minioStorage,
    IOptions<MinioSettings> minioOptions) : ICategoryQueryService
{
    private readonly MinioSettings _minio = minioOptions.Value;

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return CategoryErrors.NotFound(id);

        return await ToDtoAsync(category);
    }

    public async Task<Result<PagedResult<CategoryDto>>> GetPagedAsync(
        GetCategoriesQuery query, CancellationToken ct = default)
    {
        var queryable = categoryRepo.GetQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            queryable = queryable.Where(c => c.Name.Contains(query.SearchTerm));

        if (query.ParentId.HasValue)
            queryable = queryable.Where(c => c.ParentId == query.ParentId);

        var sorted = queryable.ApplySort(query.SortBy, query.IsAscending);
        var totalCount = await sorted.CountAsync(ct);

        var raw = await sorted
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id, c.Name, c.Description, c.ParentId,
                ParentName     = c.Parent != null ? c.Parent.Name : null,
                ChildrenCount  = c.Children.Count,
                c.IconObjectKey,
                c.CreatedAt, c.UpdatedAt
            })
            .ToListAsync(ct);

        var items = await Task.WhenAll(raw.Select(async r =>
        {
            var iconUrl = r.IconObjectKey is not null
                ? await minioStorage.GeneratePresignedUrlAsync(
                    _minio.Buckets["Categories"], r.IconObjectKey, _minio.PresignedUrlExpirySeconds)
                : null;

            return new CategoryDto(r.Id, r.Name, r.Description, r.ParentId,
                r.ParentName, r.ChildrenCount, iconUrl, r.CreatedAt, r.UpdatedAt);
        }));

        return PagedResult<CategoryDto>.Create([.. items], totalCount, query);
    }

    public async Task<Result<List<CategoryTreeDto>>> GetTreeAsync(CancellationToken ct = default)
    {
        var all = await categoryRepo.GetAllWithChildrenAsync(ct);
        var roots = all.Where(c => c.ParentId == null).ToList();
        var dtos = await Task.WhenAll(roots.Select(c => BuildTreeDtoAsync(c)));
        return dtos.ToList();
    }

    public async Task<Result<CategoryTreeDto>> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var all  = await categoryRepo.GetAllWithChildrenAsync(ct);
        var root = all.FirstOrDefault(c => c.Id == id);
        if (root is null)
            return CategoryErrors.NotFound(id);

        return await BuildTreeDtoAsync(root);
    }

    private async Task<CategoryDto> ToDtoAsync(Category c)
    {
        var iconUrl = c.IconObjectKey is not null
            ? await minioStorage.GeneratePresignedUrlAsync(
                _minio.Buckets["Categories"], c.IconObjectKey, _minio.PresignedUrlExpirySeconds)
            : null;

        return new CategoryDto(
            c.Id, c.Name, c.Description, c.ParentId,
            c.Parent?.Name, c.Children.Count, iconUrl,
            c.CreatedAt, c.UpdatedAt);
    }

    private async Task<CategoryTreeDto> BuildTreeDtoAsync(Category c)
    {
        var iconUrl = c.IconObjectKey is not null
            ? await minioStorage.GeneratePresignedUrlAsync(
                _minio.Buckets["Categories"], c.IconObjectKey, _minio.PresignedUrlExpirySeconds)
            : null;

        var children = await Task.WhenAll(c.Children.Select(child => BuildTreeDtoAsync(child)));
        return new CategoryTreeDto(c.Id, c.Name, c.Description, iconUrl, [.. children]);
    }
}
