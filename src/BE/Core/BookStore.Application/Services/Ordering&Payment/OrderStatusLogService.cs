using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Ordering_Payment;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class OrderStatusLogService : IOrderStatusLogService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderStatusLogRepository _orderStatusLogs;
        private readonly IDbSession _session;

        public OrderStatusLogService(IOrderRepository orders, IOrderStatusLogRepository orderStatusLogs, IDbSession session)
        {
            _orders = orders;
            _orderStatusLogs = orderStatusLogs;
            _session = session;
        }

        public async Task<BaseResult<IReadOnlyList<OrderStatusLogDto>>> GetByOrderAsync(
            Guid userId, Guid orderId)
        {
            // 🔐 Check order ownership
            var order = await _orders.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<IReadOnlyList<OrderStatusLogDto>>.NotFound(
                    "Order not found.");

            var logs = await _orderStatusLogs.GetByOrderIdAsync(orderId);

            return BaseResult<IReadOnlyList<OrderStatusLogDto>>.Ok(
                logs.Select(x => x.ToDto()).ToList()
            );
        }
    }
}
