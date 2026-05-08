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
    // -----------------------------------------------------------------------
    // Queries (AllowAnonymous)
    // -----------------------------------------------------------------------

    /// <summary>Get paginated flat list of categories.</summary>
    /// <response code="200">Returns paged list of CategoryDto.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetCategoriesQuery query, CancellationToken ct)
    {
        var result = await queryService.GetPagedAsync(query, ct);
        return HandlePagedResult(result);
    }

    /// <summary>Get a single category by ID.</summary>
    /// <param name="id">Category GUID.</param>
    /// <response code="200">Returns CategoryDto.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await queryService.GetByIdAsync(id, ct);
        return HandleResult(result);
    }

    /// <summary>Get the full category tree (root nodes with nested children).</summary>
    /// <response code="200">Returns nested CategoryTreeDto list.</response>
    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryTreeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await queryService.GetTreeAsync(ct);
        return HandleResult(result);
    }

    // -----------------------------------------------------------------------
    // Commands (Admin only)
    // -----------------------------------------------------------------------

    /// <summary>Create a new category.</summary>
    /// <response code="201">Category created — returns new category ID.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Parent category not found.</response>
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

    /// <summary>Update an existing category (full replace).</summary>
    /// <param name="id">Category GUID.</param>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed or circular reference detected.</response>
    /// <response code="404">Category or parent not found.</response>
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

    /// <summary>Partially update a category. Only provided (non-null) fields are changed.</summary>
    /// <param name="id">Category GUID.</param>
    /// <response code="200">Category patched.</response>
    /// <response code="400">Validation failed or circular reference detected.</response>
    /// <response code="404">Category or parent not found.</response>
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

    /// <summary>Delete a category (hard delete). Fails if category has children or books.</summary>
    /// <param name="id">Category GUID.</param>
    /// <response code="200">Category deleted.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has children or associated books.</response>
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
