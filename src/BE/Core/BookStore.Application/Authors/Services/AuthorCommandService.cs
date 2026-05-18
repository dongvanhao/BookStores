using BookStore.Application.Authors.Commands;
using BookStore.Application.Authors.IService;
using BookStore.Application.Media.Commands;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Authors.Services;

public class AuthorCommandService(
    IAuthorRepository authorRepo,
    IMediaService mediaService,
    IMediaRepository mediaRepo,
    IUnitOfWork unitOfWork) : IAuthorCommandService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxAvatarBytes = 5_242_880; // 5 MB

    public async Task<Result<Guid>> CreateAsync(CreateAuthorCommand command, CancellationToken ct = default)
    {
        if (await authorRepo.ExistsByFullNameAsync(command.FullName, ct))
            return AuthorErrors.FullNameExists;

        var author = Author.Create(command.FullName, command.Bio);
        authorRepo.Add(author);
        await unitOfWork.SaveChangesAsync(ct);
        return author.Id;
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateAuthorCommand command, CancellationToken ct = default)
    {
        var author = await authorRepo.GetByIdAsync(id, ct);
        if (author is null)
            return Result.Failure(AuthorErrors.NotFound(id));

        if (!string.Equals(author.FullName, command.FullName, StringComparison.Ordinal)
            && await authorRepo.ExistsByFullNameAsync(command.FullName, ct))
            return Result.Failure(AuthorErrors.FullNameExists);

        author.Update(command.FullName, command.Bio);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var author = await authorRepo.GetByIdAsync(id, ct);
        if (author is null)
            return Result.Failure(AuthorErrors.NotFound(id));

        if (await authorRepo.HasBooksAsync(id, ct))
            return Result.Failure(AuthorErrors.HasBooks);

        authorRepo.Remove(author);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<string>> UploadAvatarAsync(
        Guid id, IFormFile file, Guid uploadedBy, CancellationToken ct = default)
    {
        var author = await authorRepo.GetByIdAsync(id, ct);
        if (author is null)
            return AuthorErrors.NotFound(id);

        if (file.Length > MaxAvatarBytes)
            return AuthorErrors.AvatarTooLarge;

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return AuthorErrors.AvatarInvalidFormat;

        if (author.AvatarUrl is not null)
        {
            var oldMedia = await mediaRepo.GetByObjectKeyAsync(author.AvatarUrl, ct);
            if (oldMedia is not null)
                await mediaService.DeleteAsync(oldMedia.Id, uploadedBy, isAdmin: true, ct);
        }

        var uploadResult = await mediaService.UploadAsync(
            new UploadMediaCommand { File = file, Module = "authors", UploadedBy = uploadedBy }, ct);

        if (!uploadResult.IsSuccess)
            return Result.Failure<string>(uploadResult.Error);

        author.SetAvatar(uploadResult.Value.ObjectKey);
        await unitOfWork.SaveChangesAsync(ct);
        return uploadResult.Value.Url;
    }
}
