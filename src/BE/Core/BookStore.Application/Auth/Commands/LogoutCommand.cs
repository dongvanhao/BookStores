namespace BookStore.Application.Auth.Commands;

public record LogoutCommand(string RefreshToken, Guid UserId = default);
