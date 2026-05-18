namespace BookStore.Application.Authors.DTOs;

public record AuthorDetailDto(
    Guid Id,
    string FullName,
    string? Bio,
    string? AvatarUrl,
    List<AuthorBookDto> Books,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
