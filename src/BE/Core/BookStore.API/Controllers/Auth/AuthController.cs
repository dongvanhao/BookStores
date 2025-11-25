using BookStore.Application.Dtos.IdentityDto;
using BookStore.Application.IService.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthDto.RegisterDto dto)
        {
            var result = await _auth.RegisterAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            return FromResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthDto.LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            return FromResult(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] AuthDto.LogoutRequestDto dto)
        {
            var result = await _auth.LogoutAsync(dto.RefreshToken);
            return FromResult(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] AuthDto.RefreshRequestDto dto)
        {
            var result = await _auth.RefreshTokenAsync(dto.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            return FromResult(result);
        }

        //[HttpPost("confirm-email")]
        //public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
        //{
        //    var result = await _auth.ConfirmEmailAsync(userId, token);
        //    return FromResult(result);
        //}

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] AuthDto.ForgotPasswordDto dto, [FromQuery] string clientUrl)
        {
            var result = await _auth.ForgotPasswordAsync(dto.Email, clientUrl);
            return FromResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AuthDto.ResetPasswordDto dto)
        {
            var result = await _auth.ResetPasswordAsync(dto);
            return FromResult(result);
        }

        [Authorize(Roles = "Admin")] // Chỉ Admin mới gọi được API này
        [HttpPost("admin/create-user")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] AuthDto.CreateUserByAdminDto dto)
        {
            var result = await _auth.CreateUserByAdminAsync(dto);
            return FromResult(result);
        }
    }
}
