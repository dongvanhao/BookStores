namespace BookStore.Application.Categories.DTOs;

public record CategoryTreeDto(
    Guid Id,
    string Name,
    string? Description,
    List<CategoryTreeDto> Children
);
