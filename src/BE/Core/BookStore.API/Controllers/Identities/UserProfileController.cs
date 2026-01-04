using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers.Identities
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : BaseController
    {
        private readonly IUserProfileService _Service;
        public UserProfileController(IUserProfileService service)
        {
            _Service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProfileAsync()
        {
            var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

            return FromResult(await _Service.GetMyProfileAsync(userId));
        }
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile(UpdateUserProfileDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(await _Service.UpdateMyProfileAsync(userId, dto));
        }
        [Authorize]
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(
        [FromForm] UploadAvatarDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return FromResult(
                await _Service.UploadAvatarAsync(userId, dto.File)
            );
        }

    }
}
