using System.Security.Claims;
using BookStore.Application.Orders.Commands;
using BookStore.Application.Orders.DTOs;
using BookStore.Application.Orders.IService;
using BookStore.Application.Orders.Queries;
using BookStore.Shared.Common;
using BookStore.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controller;

/// <summary>Order management — place and track orders.</summary>
[Route("api/orders")]
[ApiController]
[Authorize]
public class OrdersController(
    IOrderCommandService commandService,
    IOrderQueryService   queryService) : BaseController
{
    private Guid CurrentUserId
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");


    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand cmd, CancellationToken ct)
        => HandleCreated(await commandService.CreateAsync(cmd, CurrentUserId, ct), nameof(GetById));

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => HandleResult(await commandService.ConfirmAsync(id, ct));


    [HttpPatch("{id:guid}/ship")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ship(Guid id, CancellationToken ct)
        => HandleResult(await commandService.ShipAsync(id, ct));

    [HttpPatch("{id:guid}/deliver")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken ct)
        => HandleResult(await commandService.DeliverAsync(id, ct));


    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => HandleResult(await commandService.CancelAsync(id, CurrentUserId, IsAdmin, ct));

  
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] GetOrdersQuery query, CancellationToken ct)
        => HandlePagedResult(await queryService.GetOrderHistoryAsync(query, CurrentUserId, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await queryService.GetByIdAsync(id, CurrentUserId, IsAdmin, ct));
}
