using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.Entities.Ordering___Payment;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _uow;

        public RefundService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<RefundResponseDto>> RequestRefundAsync(
            Guid userId, CreateRefundRequestDto request)
        {
            // 1️⃣ Lấy order
            var order = await _uow.Order.GetByIdAsync(request.OrderId);
            if (order == null || order.UserId != userId)
                return BaseResult<RefundResponseDto>.NotFound("Không tìm thấy đơn hàng");

            if (order.Status != "Paid")
                return BaseResult<RefundResponseDto>.Fail(
                    "Refund.InvalidOrder",
                    "Đơn hàng chưa được thanh toán",
                    ErrorType.Conflict
                );

            // 2️⃣ Lấy payment
            var payment = await _uow.PaymentTransaction.GetByOrderIdAsync(order.Id);
            if (payment == null || payment.Status != "Success")
                return BaseResult<RefundResponseDto>.Fail(
                    "Refund.InvalidPayment",
                    "Không thể hoàn tiền cho giao dịch này",
                    ErrorType.Conflict
                );

            // 3️⃣ Validate số tiền
            if (request.Amount <= 0 || request.Amount > payment.Amount)
                return BaseResult<RefundResponseDto>.Fail(
                    "Refund.InvalidAmount",
                    "Số tiền hoàn không hợp lệ",
                    ErrorType.Validation
                );

            // 4️⃣ Tạo refund (sandbox auto completed)
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentTransactionId = payment.Id,
                Amount = request.Amount,
                Reason = request.Reason,
                Status = "Completed",
                ProcessedAt = DateTime.UtcNow
            };

            await _uow.Refund.AddAsync(refund);

            // 5️⃣ Update payment + order
            payment.Status = "Refunded";
            order.Status = "Refunded";

            _uow.PaymentTransaction.Update(payment);
            _uow.Order.Update(order);
            // 6️⃣ GHI ORDER STATUS LOG (AUDIT TRẠNG THÁI)
            await _uow.OrderStatusLog.AddAsync(new OrderStatusLog
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = "Paid",
                NewStatus = "Refunded",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "User" // hoặc "System" / "Admin"
            });

            // 7️⃣ GHI ORDER HISTORY (AUDIT HÀNH ĐỘNG)
            await _uow.OrderHistory.AddAsync(new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Action = "Refund",
                Details = $"Hoàn tiền {refund.Amount:N0} - Lý do: {refund.Reason}",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync();

            return BaseResult<RefundResponseDto>.Ok(refund.ToDto());
        }

        public async Task<BaseResult<IReadOnlyList<RefundResponseDto>>> GetByOrderAsync(
            Guid userId, Guid orderId)
        {
            var order = await _uow.Order.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<IReadOnlyList<RefundResponseDto>>.NotFound();

            var payment = await _uow.PaymentTransaction.GetByOrderIdAsync(orderId);
            if (payment == null)
                return BaseResult<IReadOnlyList<RefundResponseDto>>.NotFound("Chưa có thanh toán");

            var refunds = await _uow.Refund.GetByPaymentIdAsync(payment.Id);

            return BaseResult<IReadOnlyList<RefundResponseDto>>.Ok(
                refunds.Select(x => x.ToDto()).ToList()
            );
        }
    }
}
