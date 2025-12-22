using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookFormatController : BaseController
    {
        private readonly IBookFormatService _service;
        public BookFormatController(IBookFormatService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateBookFormatRequestDto request)
            => FromResult(await _service.CreateAsync(request));

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => FromResult(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
            => FromResult(await _service.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromRoute] Guid id, UpdateBookFormatRequestDto request)
            => FromResult(await _service.UpdateAsync(id, request));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
            => FromResult(await _service.DeleteAsync(id));
    }
}
