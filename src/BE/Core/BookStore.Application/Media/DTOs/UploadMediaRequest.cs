using Microsoft.AspNetCore.Http;

namespace BookStore.Application.Media.DTOs;

public sealed class UploadMediaRequest
{
    public IFormFile File   { get; set; } = null!;
    public string    Module { get; set; } = string.Empty;
}
