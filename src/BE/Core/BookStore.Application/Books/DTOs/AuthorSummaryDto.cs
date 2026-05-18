namespace BookStore.Application.Books.DTOs;

public record AuthorSummaryDto(Guid Id, string FullName, string? AvatarUrl);
