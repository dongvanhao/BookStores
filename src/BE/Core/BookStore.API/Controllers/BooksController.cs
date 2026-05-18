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
    // -----------------------------------------------------------------------
    // Queries (AllowAnonymous)
    // -----------------------------------------------------------------------

    /// <summary>Get paginated list of books with optional filters and sorting.</summary>
    /// <response code="200">Returns paged list of BookDto.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] GetBooksQuery query, CancellationToken ct)
        => HandlePagedResult(await queryService.GetPagedAsync(query, ct));

    /// <summary>Get a single book by ID.</summary>
    /// <param name="id">Book GUID.</param>
    /// <response code="200">Returns BookDetailDto.</response>
    /// <response code="404">Book not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BookDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await queryService.GetByIdAsync(id, ct));

    // -----------------------------------------------------------------------
    // Commands (Admin only)
    // -----------------------------------------------------------------------

    /// <summary>Create a new book.</summary>
    /// <response code="201">Returns the new book's GUID.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="409">Title or ISBN already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateBookCommand cmd, CancellationToken ct)
        => HandleCreated(await commandService.CreateAsync(cmd, ct), nameof(GetById));

    /// <summary>Replace all fields of an existing book.</summary>
    /// <param name="id">Book GUID.</param>
    /// <response code="200">Update successful.</response>
    /// <response code="404">Book not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateBookCommand cmd, CancellationToken ct)
        => HandleResult(await commandService.UpdateAsync(id, cmd, ct));

    /// <summary>Partially update a book's fields.</summary>
    /// <param name="id">Book GUID.</param>
    /// <response code="200">Patch successful.</response>
    /// <response code="404">Book not found.</response>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, PatchBookCommand cmd, CancellationToken ct)
        => HandleResult(await commandService.PatchAsync(id, cmd, ct));

    /// <summary>Soft-delete a book.</summary>
    /// <param name="id">Book GUID.</param>
    /// <response code="200">Delete successful.</response>
    /// <response code="404">Book not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await commandService.DeleteAsync(id, ct));

    /// <summary>Upload or replace the book's cover image (multipart/form-data).</summary>
    /// <param name="id">Book GUID.</param>
    /// <response code="200">Returns the presigned cover URL.</response>
    /// <response code="400">Invalid file (not an image or exceeds 5 MB).</response>
    /// <response code="404">Book not found.</response>
    [HttpPost("{id:guid}/cover")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadCover(Guid id, IFormFile file, CancellationToken ct)
        => HandleResult(await commandService.UploadCoverAsync(id, file, ct));
}
