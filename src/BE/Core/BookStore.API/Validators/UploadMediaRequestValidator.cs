using BookStore.Application.Media;
using BookStore.Application.Media.DTOs;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BookStore.API.Validators;

public class UploadMediaRequestValidator : AbstractValidator<UploadMediaRequest>
{
    private static readonly string[] AllowedModules = ["books", "authors", "users"];

    public UploadMediaRequestValidator(IOptions<MinioSettings> minioOptions)
    {
        var settings = minioOptions.Value;

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.");

        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required.")
            .Must(m => AllowedModules.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Module must be one of: {string.Join(", ", AllowedModules)}.");

        RuleFor(x => x.File)
            .Must(f => f is null || settings.AllowedMimeTypes.Contains(f.ContentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("File type is not allowed.")
            .When(x => x.File is not null);

        RuleFor(x => x.File)
            .Must(f => f is null || f.Length <= settings.MaxFileSizeBytes)
            .WithMessage($"File must not exceed {settings.MaxFileSizeBytes / 1_048_576} MB.")
            .When(x => x.File is not null);
    }
}
