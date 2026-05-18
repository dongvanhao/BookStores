using BookStore.Application.Categories.Commands;
using BookStore.Application.Categories.DTOs;
using BookStore.Application.Categories.IService;
using BookStore.Application.Categories.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controller;

/// <summary>Category management — CRUD and hierarchical tree.</summary>
[Route("api/categories")]
[ApiController]
public class CategoriesController(
    ICategoryCommandService commandService,
    ICategoryQueryService queryService) : BaseController
{

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetCategoriesQuery query, CancellationToken ct)
    {
        var result = await queryService.GetPagedAsync(query, ct);
        return HandlePagedResult(result);
    }


    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await queryService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryTreeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await queryService.GetTreeAsync(ct);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await commandService.CreateAsync(cmd, ct);
        return HandleCreated(result, nameof(GetById));
    }


    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await commandService.UpdateAsync(id, cmd, ct);
        return HandleResult(result);
    }


    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, PatchCategoryCommand cmd, CancellationToken ct)
    {
        var result = await commandService.PatchAsync(id, cmd, ct);
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

}
