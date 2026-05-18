using BookStore.Application.Books.DTOs;
using BookStore.Application.Books.IService;
using BookStore.Application.Books.Queries;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Common;
using BookStore.Shared.Extensions;
using BookStore.Shared.Results;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Books.Services;

public class BookQueryService(
    IBookRepository bookRepo,
    IMinioStorageService minioService,
    IOptions<MinioSettings> minioOptions) : IBookQueryService
{
    private const string BookImagesBucket = "book-images";
    private readonly int _urlExpirySeconds = minioOptions.Value.PresignedUrlExpirySeconds;

    public async Task<Result<BookDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var book = await bookRepo.GetWithDetailsAsync(id, ct);

        if (book is null)
            return BookErrors.NotFound(id);

        var coverUrl = book.CoverUrl is not null
            ? await minioService.GeneratePresignedUrlAsync(BookImagesBucket, book.CoverUrl, _urlExpirySeconds)
            : null;

        var authors = new List<AuthorSummaryDto>();
        foreach (var ba in book.BookAuthors)
        {
            var avatarUrl = ba.Author.AvatarUrl is not null
                ? await minioService.GeneratePresignedUrlAsync("author-avatars", ba.Author.AvatarUrl, _urlExpirySeconds)
                : null;
            authors.Add(new AuthorSummaryDto(ba.Author.Id, ba.Author.FullName, avatarUrl));
        }

        var avgRating  = book.Reviews.Count > 0 ? book.Reviews.Average(r => r.Rating) : 0d;
        var reviewCount = book.Reviews.Count;

        return new BookDetailDto(
            book.Id,
            book.Title,
            book.Description,
            book.ISBN,
            book.PublishedYear,
            book.Price,
            book.StockQuantity,
            coverUrl,
            book.CategoryId,
            book.Category.Name,
            authors,
            avgRating,
            reviewCount,
            book.CreatedAt,
            book.UpdatedAt);
    }

    public async Task<Result<PagedResult<BookDto>>> GetPagedAsync(GetBooksQuery request, CancellationToken ct = default)
    {
        var query = bookRepo.GetQueryableWithDetails();

        if (request.CategoryId.HasValue)
            query = query.Where(b => b.CategoryId == request.CategoryId.Value);

        if (request.MinPrice.HasValue)
            query = query.Where(b => b.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(b => b.Price <= request.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(b => b.Title.Contains(request.SearchTerm)
                                  || b.ISBN.Contains(request.SearchTerm));

        // Project to a flat intermediate type before paging (avoids loading unused nav props)
        var projectedQuery = query
            .ApplySort(request.SortBy, request.IsAscending)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.ISBN,
                b.PublishedYear,
                b.Price,
                b.StockQuantity,
                b.CoverUrl,
                b.CategoryId,
                CategoryName = b.Category.Name,
                AuthorNames  = b.BookAuthors.Select(ba => ba.Author.FullName).ToList(),
                b.CreatedAt
            });

        var paged = await projectedQuery.ToPagedResultAsync(request, ct);

        // Resolve presigned URLs after materialization (can't call async in LINQ tree)
        var dtos = new List<BookDto>(paged.Items.Count);
        foreach (var item in paged.Items)
        {
            var coverUrl = item.CoverUrl is not null
                ? await minioService.GeneratePresignedUrlAsync(BookImagesBucket, item.CoverUrl, _urlExpirySeconds)
                : null;

            dtos.Add(new BookDto(
                item.Id,
                item.Title,
                item.ISBN,
                item.PublishedYear,
                item.Price,
                item.StockQuantity,
                coverUrl,
                item.CategoryId,
                item.CategoryName,
                item.AuthorNames,
                item.CreatedAt));
        }

        return PagedResult<BookDto>.Create(dtos, paged.TotalCount, request);
    }
}
