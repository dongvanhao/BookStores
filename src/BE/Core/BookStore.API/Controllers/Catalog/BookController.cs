using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : BaseController
    {
        private readonly IBookService _Service;
        public BookController(IBookService Service)
        {
            _Service = Service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateBookRequestDto request)
            => FromResult(await _Service.CreateAsync(request));

        [HttpGet]
        public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => FromResult(await _Service.GetListAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
            => FromResult(await _Service.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateBookRequestDto request)
            => FromResult(await _Service.UpdateAsync(id, request));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
            => FromResult(await _Service.DeleteAsync(id));
    }
}
