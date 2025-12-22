using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookMetadataController : BaseController
    {
        private readonly IBookMetadataService _service;
        public BookMetadataController(IBookMetadataService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
        Guid bookId, CreateBookMetadataRequestDto request)
        => FromResult(await _service.CreateAsync(bookId, request));

        [HttpGet]
        public async Task<IActionResult> Get(Guid bookId)
            => FromResult(await _service.GetByBookAsync(bookId));

        [HttpPut("{metadataId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            Guid bookId, Guid metadataId, UpdateBookMetadataRequestDto request)
            => FromResult(await _service.UpdateAsync(bookId, metadataId, request));

        [HttpDelete("{metadataId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid bookId, Guid metadataId)
            => FromResult(await _service.DeleteAsync(bookId, metadataId));
    }
}
