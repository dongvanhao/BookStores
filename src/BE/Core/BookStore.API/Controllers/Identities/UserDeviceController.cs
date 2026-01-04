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
    public class UserDeviceController : BaseController
    {
        private readonly IUserDeviceService _service;

        public UserDeviceController(IUserDeviceService service)
        {
            _service = service;
        }

        private Guid UserId =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetMyDevices()
            => FromResult(await _service.GetMyDevicesAsync(UserId));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id)
            => FromResult(await _service.RemoveAsync(UserId, id));
    }
}
