using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Media.Commands;

public sealed record UploadMediaCommand
{
    public IFormFile File       { get; init; } = null!;
    public string    Module     { get; init; } = string.Empty;
    public Guid      UploadedBy { get; init; }
}
