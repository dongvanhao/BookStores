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
        private readonly IBookService _bookService;
        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
       [FromForm] CreateBookRequest request,
       IFormFile? coverImage)
       => FromResult(await _bookService.CreateAsync(request, coverImage));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
            => FromResult(await _bookService.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateBookRequest request)
            => FromResult(await _bookService.UpdateAsync(id, request));

        [HttpPut("{id}/cover")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCover(Guid id, IFormFile coverImage)
            => FromResult(await _bookService.UpdateCoverAsync(id, coverImage));

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
            => FromResult(await _bookService.DeleteAsync(id));
    }
}
