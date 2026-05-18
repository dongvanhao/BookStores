using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Categories.Commands;

public sealed class UploadCategoryIconCommand
{
    public Guid       CategoryId { get; init; }
    public IFormFile  File       { get; init; } = null!;
    public Guid       UploadedBy { get; init; }
}
