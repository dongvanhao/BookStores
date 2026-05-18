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

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuthorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAuthorsQuery query, CancellationToken ct)
    {
        var result = await queryService.GetPagedAsync(query, ct);
        return HandlePagedResult(result);
    }


    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthorDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthorDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await queryService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }


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
