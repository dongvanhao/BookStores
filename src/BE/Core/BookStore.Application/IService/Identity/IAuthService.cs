using BookStore.Application.Dtos.IdentityDto;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BookStore.Application.Dtos.IdentityDto.AuthDto;

namespace BookStore.Application.IService.Identity
{
    public interface IAuthService
    {
        Task<BaseResult<string>> RegisterAsync(RegisterDto dto, string originIp);
        Task<BaseResult<AuthResponseDto>> LoginAsync(LoginDto dto, string originIp);
        Task<BaseResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string originIp);
        Task<BaseResult<string>> LogoutAsync(string refreshTokenPlain);
        //Task<BaseResult<string>> ConfirmEmailAsync(Guid userId, string tokenPlain);
        Task<BaseResult<string>> ForgotPasswordAsync(string email, string clientUrl);
        Task<BaseResult<string>> ResetPasswordAsync(ResetPasswordDto dto);
        Task<BaseResult<string>> CreateUserByAdminAsync(AuthDto.CreateUserByAdminDto dto);
    }
}
