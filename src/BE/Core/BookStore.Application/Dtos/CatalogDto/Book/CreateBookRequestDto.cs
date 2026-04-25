using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public record CreateBookRequestDto(
        [Required][MaxLength(500)] string Title,
        [Required][MaxLength(20)] string ISBN,
        [MaxLength(5000)] string? Description,
        [Range(1000, 2100)] int PublicationYear,
        [MaxLength(50)] string? Language,
        [MaxLength(50)] string? Edition,
        [Range(1, 10000)] int? PageCount,
        [Required] Guid PublisherId,
        List<Guid> AuthorIds,
        List<Guid> CategoryIds
    );
}
