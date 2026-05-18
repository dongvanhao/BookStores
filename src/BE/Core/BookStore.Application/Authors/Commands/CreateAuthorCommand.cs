namespace BookStore.Application.Authors.Commands;

public record CreateAuthorCommand(string FullName, string? Bio);
