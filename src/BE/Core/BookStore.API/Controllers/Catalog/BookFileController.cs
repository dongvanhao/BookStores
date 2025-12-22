using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Domain.IRepository.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookFileController : BaseController
    {
        private readonly IBookFileService _service;
        public BookFileController(IBookFileService service)
        {
            _service = service;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            Guid guid,
            IFormFile file,
            [FromForm] UploadBookFileRequestDto request)
            => FromResult(await _service.UploadAsync(guid, file, request));

        [HttpGet]
        public async Task<IActionResult> GetFiles(Guid BookId)
            => FromResult(await _service.GetAllByBookIdAsync(BookId));

        [HttpDelete("{fileId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid bookId, Guid fileId)
            => FromResult(await _service.DeleteAsync(bookId, fileId));
    }
}
