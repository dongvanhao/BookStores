namespace BookStore.Application.Categories.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentId,
    string? ParentName,
    int ChildrenCount,
    string? IconUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
