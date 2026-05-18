using BookStore.Application.Authors.Commands;
using BookStore.Application.Authors.DTOs;
using BookStore.Application.Authors.IService;
using BookStore.Application.Authors.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controller;

/// <summary>Author management — CRUD and avatar upload.</summary>
[Route("api/authors")]
[ApiController]
public class AuthorsController(
    IAuthorQueryService queryService,
    IAuthorCommandService commandService) : BaseController
{
    // -----------------------------------------------------------------------
    // Queries (AllowAnonymous)
    // -----------------------------------------------------------------------

    /// <summary>Get paginated list of authors.</summary>
    /// <response code="200">Returns paged list of AuthorDto.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuthorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAuthorsQuery query, CancellationToken ct)
    {
        var result = await queryService.GetPagedAsync(query, ct);
        return HandlePagedResult(result);
    }

    /// <summary>Get author detail by ID, including linked books.</summary>
    /// <param name="id">Author GUID.</param>
    /// <response code="200">Returns AuthorDetailDto.</response>
    /// <response code="404">Author not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthorDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthorDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await queryService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    // -----------------------------------------------------------------------
    // Commands (Admin only)
    // -----------------------------------------------------------------------

    /// <summary>Create a new author.</summary>
    /// <response code="201">Author created — returns new author ID.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Author with this name already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateAuthorCommand command, CancellationToken ct)
    {
        var result = await commandService.CreateAsync(command, ct);
        return HandleCreated(result, nameof(GetById));
    }

    /// <summary>Update an existing author (full replace).</summary>
    /// <param name="id">Author GUID.</param>
    /// <response code="200">Author updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Author not found.</response>
    /// <response code="409">Author with this name already exists.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateAuthorCommand command, CancellationToken ct)
    {
        var result = await commandService.UpdateAsync(id, command, ct);
        return HandleResult(result);
    }

    /// <summary>Delete an author (hard delete). Fails if linked to books.</summary>
    /// <param name="id">Author GUID.</param>
    /// <response code="200">Author deleted.</response>
    /// <response code="404">Author not found.</response>
    /// <response code="409">Author is linked to one or more books.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await commandService.DeleteAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Upload or replace author avatar (jpg/jpeg/png/webp, max 5 MB).</summary>
    /// <param name="id">Author GUID.</param>
    /// <response code="200">Returns presigned avatar URL.</response>
    /// <response code="400">File too large or invalid format.</response>
    /// <response code="404">Author not found.</response>
    [HttpPatch("{id:guid}/avatar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file, CancellationToken ct)
    {
        var uploadedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await commandService.UploadAvatarAsync(id, file, uploadedBy, ct);
        return HandleResult(result);
    }
}
