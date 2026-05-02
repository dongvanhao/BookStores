namespace BookStore.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password, string FullName);
