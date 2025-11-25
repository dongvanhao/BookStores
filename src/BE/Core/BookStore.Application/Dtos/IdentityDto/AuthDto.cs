using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.IdentityDto
{
    public class AuthDto
    {
        public record RegisterDto(string Email, string Password, string? FullName);
        public record LoginDto(string Email, string Password);
        public record AuthResponseDto(string AccessToken, string RefreshToken,string email, DateTime ExpiresAt);
        public record RefreshRequestDto(string RefreshToken);
        public record LogoutRequestDto(string RefreshToken);
        public record ForgotPasswordDto(string Email);
        public record ResetPasswordDto(Guid UserId, string Token, string NewPassword);
        public record CreateUserByAdminDto(string Email, string Password, string RoleName);
    }
}
