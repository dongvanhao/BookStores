using BookStore.Shared.Results;

namespace BookStore.Application.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

    public static readonly Error RefreshTokenNotFound =
        Error.NotFound("Auth.RefreshTokenNotFound", "Refresh token not found.");

    public static readonly Error UserNotFound =
        Error.NotFound("Auth.UserNotFound", "User not found.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Auth.EmailAlreadyExists", "An account with this email already exists.");

    public static readonly Error RegistrationFailed =
        Error.Failure("Auth.RegistrationFailed", "Failed to create account.");
}
