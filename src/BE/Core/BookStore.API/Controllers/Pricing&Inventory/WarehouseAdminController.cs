using BookStore.Application.Dtos.Pricing_Inventory.Warehouse;
using BookStore.Application.IService.Pricing_Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Pricing_Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseAdminController : BaseController
    {
        private readonly IWarehouseService _service;

        public WarehouseAdminController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll()
            => FromResult(await _service.GetAllAsync());

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Get(Guid id)
            => FromResult(await _service.GetByIdAsync(id));

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(WarehouseRequestDto dto)
            => FromResult(await _service.CreateAsync(dto));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(Guid id, WarehouseRequestDto dto)
            => FromResult(await _service.UpdateAsync(id, dto));
    }
}
