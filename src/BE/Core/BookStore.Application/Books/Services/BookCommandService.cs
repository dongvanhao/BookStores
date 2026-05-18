using BookStore.Application.Books.Commands;
using BookStore.Application.Books.IService;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Books.Services;

public class BookCommandService(
    IBookRepository bookRepo,
    ICategoryRepository categoryRepo,
    IMinioStorageService minioService,
    IUnitOfWork unitOfWork) : IBookCommandService
{
    private const string BookImagesBucket = "book-images";
    private const long MaxCoverFileSizeBytes = 5_242_880; // 5 MB

    public async Task<Result<Guid>> CreateAsync(CreateBookCommand cmd, CancellationToken ct = default)
    {
        if (await bookRepo.ExistsByTitleAsync(cmd.Title, ct))
            return BookErrors.TitleExists;

        if (await bookRepo.ExistsByISBNAsync(cmd.ISBN, ct))
            return BookErrors.ISBNExists(cmd.ISBN);

        var category = await categoryRepo.GetByIdAsync(cmd.CategoryId, ct);
        if (category is null)
            return BookErrors.CategoryNotFound(cmd.CategoryId);

        var book = Book.Create(cmd.Title, cmd.Description, cmd.ISBN, cmd.PublishedYear, cmd.Price, cmd.StockQuantity, cmd.CategoryId);
        bookRepo.Add(book);

        foreach (var authorId in cmd.AuthorIds)
            bookRepo.AddBookAuthor(new BookAuthor { BookId = book.Id, AuthorId = authorId });

        await unitOfWork.SaveChangesAsync(ct);
        return book.Id;
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateBookCommand cmd, CancellationToken ct = default)
    {
        var book = await bookRepo.GetByIdAsync(id, ct);
        if (book is null)
            return Result.Failure(BookErrors.NotFound(id));

        if (!string.Equals(book.Title, cmd.Title, StringComparison.Ordinal)
            && await bookRepo.ExistsByTitleAsync(cmd.Title, ct))
            return Result.Failure(BookErrors.TitleExists);

        if (!string.Equals(book.ISBN, cmd.ISBN, StringComparison.Ordinal)
            && await bookRepo.ExistsByISBNAsync(cmd.ISBN, ct))
            return Result.Failure(BookErrors.ISBNExists(cmd.ISBN));

        var category = await categoryRepo.GetByIdAsync(cmd.CategoryId, ct);
        if (category is null)
            return Result.Failure(BookErrors.CategoryNotFound(cmd.CategoryId));

        book.Update(cmd.Title, cmd.Description, cmd.ISBN, cmd.PublishedYear, cmd.Price, cmd.StockQuantity, cmd.CategoryId);

        await SyncAuthorsAsync(id, cmd.AuthorIds, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PatchAsync(Guid id, PatchBookCommand cmd, CancellationToken ct = default)
    {
        var book = await bookRepo.GetByIdAsync(id, ct);
        if (book is null)
            return Result.Failure(BookErrors.NotFound(id));

        var newTitle = cmd.Title ?? book.Title;
        var newDesc  = cmd.Description ?? book.Description;
        var newIsbn  = cmd.ISBN ?? book.ISBN;
        var newYear  = cmd.PublishedYear ?? book.PublishedYear;
        var newPrice = cmd.Price ?? book.Price;
        var newStock = cmd.StockQuantity ?? book.StockQuantity;
        var newCatId = cmd.CategoryId ?? book.CategoryId;

        if (cmd.Title is not null && !string.Equals(book.Title, cmd.Title, StringComparison.Ordinal)
            && await bookRepo.ExistsByTitleAsync(cmd.Title, ct))
            return Result.Failure(BookErrors.TitleExists);

        if (cmd.ISBN is not null && !string.Equals(book.ISBN, cmd.ISBN, StringComparison.Ordinal)
            && await bookRepo.ExistsByISBNAsync(cmd.ISBN, ct))
            return Result.Failure(BookErrors.ISBNExists(cmd.ISBN));

        if (cmd.CategoryId.HasValue && cmd.CategoryId.Value != book.CategoryId)
        {
            var category = await categoryRepo.GetByIdAsync(cmd.CategoryId.Value, ct);
            if (category is null)
                return Result.Failure(BookErrors.CategoryNotFound(cmd.CategoryId.Value));
        }

        book.Update(newTitle, newDesc, newIsbn, newYear, newPrice, newStock, newCatId);

        if (cmd.AuthorIds is not null)
            await SyncAuthorsAsync(id, cmd.AuthorIds, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var book = await bookRepo.GetByIdAsync(id, ct);
        if (book is null)
            return Result.Failure(BookErrors.NotFound(id));

        book.SoftDelete();
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<string>> UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (!file.ContentType.StartsWith("image/") || file.Length > MaxCoverFileSizeBytes)
            return BookErrors.InvalidCoverFile;

        var book = await bookRepo.GetByIdAsync(id, ct);
        if (book is null)
            return BookErrors.NotFound(id);

        if (!string.IsNullOrEmpty(book.CoverUrl))
            await minioService.DeleteAsync(BookImagesBucket, book.CoverUrl, ct);

        var objectKey = $"books/{id}/{file.FileName}";
        await using var stream = file.OpenReadStream();
        await minioService.UploadAsync(BookImagesBucket, objectKey, stream, file.ContentType, file.Length, ct);

        book.SetCover(objectKey);
        await unitOfWork.SaveChangesAsync(ct);

        var url = await minioService.GeneratePresignedUrlAsync(BookImagesBucket, objectKey, 3600);
        return url;
    }

    private async Task SyncAuthorsAsync(Guid bookId, IReadOnlyList<Guid> newAuthorIds, CancellationToken ct)
    {
        var existingIds = await bookRepo.GetAuthorIdsAsync(bookId, ct);

        var toRemove = existingIds.Except(newAuthorIds).ToList();
        var toAdd    = newAuthorIds.Except(existingIds).ToList();

        foreach (var authorId in toRemove)
            bookRepo.RemoveBookAuthor(new BookAuthor { BookId = bookId, AuthorId = authorId });

        foreach (var authorId in toAdd)
            bookRepo.AddBookAuthor(new BookAuthor { BookId = bookId, AuthorId = authorId });
    }
}
