using BookStore.Application.Dtos.CatalogDto.Publisher;
using BookStore.Application.IService.Catalog.Publisher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublisherController : BaseController
    {
        private readonly IPublisherService _Service;

        public PublisherController(IPublisherService service)
        {
            _Service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreatePublisherDto request)
        => FromResult(await _Service.CreateAsync(request));

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => FromResult(await _Service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
            => FromResult(await _Service.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdatePublisherRequestDto request)
            => FromResult(await _Service.UpdateAsync(id, request));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
            => FromResult(await _Service.DeleteAsync(id));
    }
}
