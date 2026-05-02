namespace BookStore.Application.Auth;

public class JwtOptions
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessExpiryMinutes { get; init; } = 15;
    public int RefreshExpiryDays { get; init; } = 7;
}
