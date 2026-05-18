using BookStore.Application.Authors.Commands;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Authors.IService;

public interface IAuthorCommandService
{
    Task<Result<Guid>> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, UpdateAuthorCommand command, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<string>> UploadAvatarAsync(Guid id, IFormFile file, Guid uploadedBy, CancellationToken ct = default);
}
