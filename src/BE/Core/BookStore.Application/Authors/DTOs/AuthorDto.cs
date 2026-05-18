namespace BookStore.Application.Authors.DTOs;

public record AuthorDto(
    Guid Id,
    string FullName,
    string? Bio,
    string? AvatarUrl,
    int BookCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
