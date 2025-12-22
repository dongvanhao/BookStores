using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookAuthorController : BaseController
    {
        private readonly IBookAuthorService _service;

        public BookAuthorController(IBookAuthorService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(
            Guid bookId,
            AddAuthorsToBookRequest request)
            => FromResult(await _service.AddAuthorsAsync(bookId, request.AuthorIds));

        [HttpGet]
        public async Task<IActionResult> Get(Guid bookId)
            => FromResult(await _service.GetAuthorsAsync(bookId));

        [HttpDelete("{authorId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(Guid bookId, Guid authorId)
            => FromResult(await _service.RemoveAuthorAsync(bookId, authorId));
    }
}
