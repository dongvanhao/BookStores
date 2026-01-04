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
    public class UserAddressController : BaseController
    {
        private readonly IUserAddressService _service;

        public UserAddressController(IUserAddressService service)
        {
            _service = service;
        }

        private Guid UserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Get()
            => FromResult(await _service.GetMyAsync(UserId));

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserAddressDto dto)
            => FromResult(await _service.CreateAsync(UserId, dto));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserAddressDto dto)
            => FromResult(await _service.UpdateAsync(UserId, id, dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => FromResult(await _service.DeleteAsync(UserId, id));

        [HttpPut("{id}/default")]
        public async Task<IActionResult> SetDefault(Guid id)
            => FromResult(await _service.SetDefaultAsync(UserId, id));
    }
}
