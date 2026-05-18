using BookStore.API.Controller;
using BookStore.Application.Media.Commands;
using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.IService;
using BookStore.Application.Media.Queries;
using BookStore.Domain.Enums;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookStore.API.Controllers;

[Route("api/media")]
[Authorize]
public class MediaController(
    IMediaService mediaService,
    IMediaQueryService mediaQueryService) : BaseController
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim is missing."));

    private bool IsAdmin => User.IsInRole("Admin");


    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Upload([FromForm] UploadMediaRequest request, CancellationToken ct)
    {
        var cmd = new UploadMediaCommand
        {
            File       = request.File,
            Module     = request.Module,
            UploadedBy = CurrentUserId
        };

        var result = await mediaService.UploadAsync(cmd, ct);
        return HandleCreated(result, nameof(GetById), dto => new { id = dto.Id });
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediaQueryService.GetByIdAsync(id, CurrentUserId, IsAdmin, ct);
        return HandleResult(result);
    }


    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediaService.DeleteAsync(id, CurrentUserId, IsAdmin, ct);
        if (result.IsSuccess)
            return NoContent();

        return HandleResult(result);
    }


    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<MediaListResponse>), 200)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? module,
        [FromQuery] MediaType? type,
        [FromQuery] DateTime? before,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        var query = new GetMediaListQuery
        {
            Module = module,
            Type   = type,
            Before = before,
            Limit  = limit
        };

        var result = await mediaQueryService.GetListAsync(query, CurrentUserId, IsAdmin, ct);
        return HandleResult(result);
    }
}
