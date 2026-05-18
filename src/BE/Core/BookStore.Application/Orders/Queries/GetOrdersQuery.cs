using BookStore.Domain.Enums;
using BookStore.Shared.Common;

namespace BookStore.Application.Orders.Queries;

public sealed class GetOrdersQuery : QueryParams
{
    public OrderStatus? Status { get; set; }
}
