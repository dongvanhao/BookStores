namespace BookStore.Application.Authors.Commands;

public record UpdateAuthorCommand(string FullName, string? Bio);
