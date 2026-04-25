using System.ComponentModel.DataAnnotations;

namespace BookStore.Application.Dtos.IdentityDto
{
    public class AuthDto
    {
        public record RegisterDto(
            [Required][EmailAddress][MaxLength(256)] string Email,
            [Required][MinLength(8)][MaxLength(128)] string Password,
            [MaxLength(100)] string? FullName
        );

        public record LoginDto(
            [Required][EmailAddress] string Email,
            [Required] string Password
        );

        public record AuthResponseDto(
            string AccessToken,
            string RefreshToken,
            string Email,
            DateTime ExpiresAt
        );

        public record RefreshRequestDto(
            [Required] string RefreshToken
        );

        public record LogoutRequestDto(
            [Required] string RefreshToken
        );

        public record ForgotPasswordDto(
            [Required][EmailAddress] string Email
        );

        public record ResetPasswordDto(
            [Required] Guid UserId,
            [Required] string Token,
            [Required][MinLength(8)][MaxLength(128)] string NewPassword
        );

        public record CreateUserByAdminDto(
            [Required][EmailAddress][MaxLength(256)] string Email,
            [Required][MinLength(8)][MaxLength(128)] string Password,
            [Required][MaxLength(50)] string RoleName
        );
    }
}
