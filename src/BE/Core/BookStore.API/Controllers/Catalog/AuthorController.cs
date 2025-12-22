using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Catalog
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorController : BaseController
    {
        private readonly IAuthorService _Service;

        public AuthorController(IAuthorService service)
        {
            _Service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAuthorRequestDto request)
        => FromResult(await _Service.CreateAsync(request));

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => FromResult(await _Service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
            => FromResult(await _Service.GetByIdAsync(id));

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, UpdateAuthorRequestDto request)
            => FromResult(await _Service.UpdateAsync(id, request));
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
            => FromResult(await _Service.DeleteAsync(id));
    }

}
