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
    public class OrderHistoryService : IOrderHistoryService
    {
        private readonly IUnitOfWork _uow;

        public OrderHistoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<OrderHistoryDto>>> GetByOrderAsync(
            Guid userId, Guid orderId)
        {
            // 🔐 Check ownership
            var order = await _uow.Order.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<IReadOnlyList<OrderHistoryDto>>.NotFound(
                    "Không tìm thấy đơn hàng");

            var histories = await _uow.OrderHistory.GetByOrderIdAsync(orderId);

            return BaseResult<IReadOnlyList<OrderHistoryDto>>.Ok(
                histories.Select(x => x.ToDto()).ToList()
            );
        }
    }
}
