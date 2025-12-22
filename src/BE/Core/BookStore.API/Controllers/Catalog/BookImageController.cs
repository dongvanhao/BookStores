using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookImageController : BaseController
    {
        private readonly IBookImageService _service;
        public BookImageController(IBookImageService service)
        {
            _service = service;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(Guid bookId, IFormFile file, [FromForm] UploadBookImageRequestDto request)
            => FromResult(await _service.UploadAsync(bookId, file, request));

        [HttpGet]
        public async Task<IActionResult> GetImages(Guid bookId)
            => FromResult(await _service.GetByBookIdAsync(bookId));

        [HttpPut("{imageId}/set-cover")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetCover(Guid bookId, Guid imageId)
            => FromResult(await _service.SetCoverAsync(bookId ,imageId));

        [HttpPut("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reorder(Guid bookId, [FromBody] ReorderBookImageRequestDto request)
            => FromResult(await _service.ReorderAsync(bookId, request.ImageIds));

        [HttpDelete("{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid bookId, Guid imageId)
            => FromResult(await _service.DeleteAsync(bookId, imageId));
    }
}
