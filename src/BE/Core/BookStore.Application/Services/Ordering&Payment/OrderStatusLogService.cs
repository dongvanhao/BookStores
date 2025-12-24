using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class OrderStatusLogService : IOrderStatusLogService
    {
        private readonly IUnitOfWork _uow;

        public OrderStatusLogService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<OrderStatusLogDto>>> GetByOrderAsync(
            Guid userId, Guid orderId)
        {
            // 🔐 Check order ownership
            var order = await _uow.Order.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<IReadOnlyList<OrderStatusLogDto>>.NotFound(
                    "Không tìm thấy đơn hàng");

            var logs = await _uow.OrderStatusLog.GetByOrderIdAsync(orderId);

            return BaseResult<IReadOnlyList<OrderStatusLogDto>>.Ok(
                logs.Select(x => x.ToDto()).ToList()
            );
        }
    }
}
