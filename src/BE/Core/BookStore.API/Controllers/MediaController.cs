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

    /// <summary>Upload một file lên MinIO và lưu metadata vào DB.</summary>
    /// <response code="201">Upload thành công, trả về MediaDto kèm presigned URL.</response>
    /// <response code="400">File không hợp lệ (sai MIME type hoặc quá kích thước).</response>
    /// <response code="401">Chưa xác thực.</response>
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

    /// <summary>Lấy thông tin một media item kèm presigned URL mới.</summary>
    /// <param name="id">Media GUID</param>
    /// <response code="200">Trả về MediaDto.</response>
    /// <response code="403">Không phải owner hoặc Admin.</response>
    /// <response code="404">Media không tồn tại.</response>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediaQueryService.GetByIdAsync(id, CurrentUserId, IsAdmin, ct);
        return HandleResult(result);
    }

    /// <summary>Xóa một media item (kiểm tra ownership).</summary>
    /// <param name="id">Media GUID</param>
    /// <response code="204">Xóa thành công.</response>
    /// <response code="403">Không phải owner hoặc Admin.</response>
    /// <response code="404">Media không tồn tại.</response>
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

    /// <summary>Lấy danh sách media với cursor pagination.</summary>
    /// <response code="200">Trả về danh sách MediaDto kèm cursor metadata.</response>
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
