using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookCategoryController : BaseController
    {
        private readonly IBookCategoryService _service;

        public BookCategoryController(IBookCategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(
            Guid bookId,
            AddCategoriesToBookRequest request)
            => FromResult(await _service.AddCategoriesAsync(bookId, request.CategoryIds));

        [HttpGet]
        public async Task<IActionResult> Get(Guid bookId)
            => FromResult(await _service.GetCategoriesAsync(bookId));

        [HttpDelete("{categoryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(Guid bookId, Guid categoryId)
            => FromResult(await _service.RemoveCategoryAsync(bookId, categoryId));
    }
}
