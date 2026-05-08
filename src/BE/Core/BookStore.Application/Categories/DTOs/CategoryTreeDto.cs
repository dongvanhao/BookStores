namespace BookStore.Application.Categories.DTOs;

public record CategoryTreeDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    List<CategoryTreeDto> Children
);
