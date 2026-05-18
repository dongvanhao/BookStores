using BookStore.Application.Books.Commands;
using BookStore.Application.Books.DTOs;
using BookStore.Application.Books.IService;
using BookStore.Application.Books.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controller;

/// <summary>Book catalog — CRUD, cover upload, and query.</summary>
[Route("api/books")]
[ApiController]
public class BooksController(
    IBookCommandService commandService,
    IBookQueryService queryService) : BaseController
{

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] GetBooksQuery query, CancellationToken ct)
        => HandlePagedResult(await queryService.GetPagedAsync(query, ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BookDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await queryService.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateBookCommand cmd, CancellationToken ct)
        => HandleCreated(await commandService.CreateAsync(cmd, ct), nameof(GetById));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateBookCommand cmd, CancellationToken ct)
        => HandleResult(await commandService.UpdateAsync(id, cmd, ct));

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, PatchBookCommand cmd, CancellationToken ct)
        => HandleResult(await commandService.PatchAsync(id, cmd, ct));


    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await commandService.DeleteAsync(id, ct));


    [HttpPost("{id:guid}/cover")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadCover(Guid id, IFormFile file, CancellationToken ct)
        => HandleResult(await commandService.UploadCoverAsync(id, file, ct));
}
