using BookStore.Application.Books.Commands;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Books.IService;

public interface IBookCommandService
{
    Task<Result<Guid>>   CreateAsync(CreateBookCommand cmd, CancellationToken ct = default);
    Task<Result>         UpdateAsync(Guid id, UpdateBookCommand cmd, CancellationToken ct = default);
    Task<Result>         PatchAsync(Guid id, PatchBookCommand cmd, CancellationToken ct = default);
    Task<Result>         DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<string>> UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default);
}
