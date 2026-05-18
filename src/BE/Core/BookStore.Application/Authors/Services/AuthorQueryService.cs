using BookStore.Application.Authors.DTOs;
using BookStore.Application.Authors.IService;
using BookStore.Application.Authors.Queries;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Common;
using BookStore.Shared.Extensions;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Authors.Services;

public class AuthorQueryService(
    IAuthorRepository authorRepo,
    IMinioStorageService minioStorage,
    IOptions<MinioSettings> minioOptions) : IAuthorQueryService
{
    private readonly MinioSettings _minio = minioOptions.Value;

    public async Task<Result<PagedResult<AuthorDto>>> GetPagedAsync(
        GetAuthorsQuery query, CancellationToken ct = default)
    {
        var queryable = authorRepo.GetQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            queryable = queryable.Where(a => a.FullName.Contains(query.SearchTerm));

        var sorted = queryable.ApplySort(query.SortBy, query.IsAscending);
        var totalCount = await sorted.CountAsync(ct);

        var raw = await sorted
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new
            {
                a.Id, a.FullName, a.Bio, a.AvatarUrl,
                BookCount = a.BookAuthors.Count,
                a.CreatedAt, a.UpdatedAt
            })
            .ToListAsync(ct);

        var items = await Task.WhenAll(raw.Select(async r =>
        {
            var avatarUrl = r.AvatarUrl is not null
                ? await minioStorage.GeneratePresignedUrlAsync(
                    _minio.Buckets["authors"], r.AvatarUrl, _minio.PresignedUrlExpirySeconds)
                : null;

            return new AuthorDto(r.Id, r.FullName, r.Bio, avatarUrl, r.BookCount, r.CreatedAt, r.UpdatedAt);
        }));

        return PagedResult<AuthorDto>.Create([.. items], totalCount, query);
    }

    public async Task<Result<AuthorDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var author = await authorRepo.GetByIdWithBooksAsync(id, ct);
        if (author is null)
            return AuthorErrors.NotFound(id);

        var avatarUrl = author.AvatarUrl is not null
            ? await minioStorage.GeneratePresignedUrlAsync(
                _minio.Buckets["authors"], author.AvatarUrl, _minio.PresignedUrlExpirySeconds)
            : null;

        var books = author.BookAuthors
            .Select(ba => new AuthorBookDto(ba.Book.Id, ba.Book.Title, ba.Book.ISBN))
            .ToList();

        return new AuthorDetailDto(author.Id, author.FullName, author.Bio, avatarUrl, books, author.CreatedAt, author.UpdatedAt);
    }
}
