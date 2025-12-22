using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Domain.IRepository.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody]CreateCategoryRequestDto request)
        {
            var result = await _service.CreateAsync(request);
            return FromResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => FromResult(await _service.GetAllAsync());

        [HttpGet("tree")]

        public async Task<IActionResult> GetTree()
            => FromResult(await _service.GetTreeAsync());

        [HttpGet("{id}")]

        public async Task<IActionResult> GetById(Guid id)
            => FromResult(await _service.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateCategoryRequestDto request)
            => FromResult(await _service.UpdateAsync(id, request));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
            => FromResult(await _service.DeleteAsync(id));
    }
}
