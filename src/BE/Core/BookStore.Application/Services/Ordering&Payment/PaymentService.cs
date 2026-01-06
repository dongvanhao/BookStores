using BookStore.Application.Dtos.Ordering_Payment.Payment;
using BookStore.Application.Dtos.Ordering_Payment.PaymentRequestDto;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Domain.Entities.Ordering_Payment;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;

        public PaymentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<PaymentResponseDto>> PayAsync(
            Guid userId, PaymentRequestDto request)
        {
            // 1️⃣ Lấy order
            var order = await _uow.Order.GetByIdAsync(request.OrderId);
            if (order == null || order.UserId != userId)
                return BaseResult<PaymentResponseDto>.NotFound("Không tìm thấy đơn hàng");

            if (order.Status != "Pending")
                return BaseResult<PaymentResponseDto>.Fail(
                    "Payment.InvalidOrder",
                    "Đơn hàng không thể thanh toán",
                    ErrorType.Conflict
                );

            // 2️⃣ Check payment tồn tại
            var existing = await _uow.PaymentTransaction.GetByOrderIdAsync(order.Id);
            if (existing != null)
                return BaseResult<PaymentResponseDto>.Fail(
                    "Payment.Existed",
                    "Đơn hàng đã có giao dịch thanh toán",
                    ErrorType.Conflict
                );

            // 3️⃣ Tạo transaction (sandbox)
            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = "Sandbox",
                PaymentMethod = "Online",
                TransactionCode = $"SBX-{Guid.NewGuid():N}",
                Amount = order.FinalAmount,
                Status = request.IsSuccess ? "Success" : "Failed",
                PaidAt = request.IsSuccess ? DateTime.UtcNow : null
            };

            await _uow.PaymentTransaction.AddAsync(payment);

            // 4️⃣ Update order nếu success
            if (request.IsSuccess)
            {
                var oldStatus = order.Status;

                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;
                _uow.Order.Update(order);

                // ✅ ORDER STATUS LOG
                await _uow.OrderStatusLog.AddAsync(new OrderStatusLog
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = "Paid",
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = "User"
                });

                // ✅ ORDER HISTORY
                await _uow.OrderHistory.AddAsync(new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Action = "Payment",
                    Details = $"Thanh toán thành công {payment.Amount:N0}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // (Optional nhưng rất đẹp)
                await _uow.OrderHistory.AddAsync(new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Action = "PaymentFailed",
                    Details = "Thanh toán thất bại (sandbox)",
                    CreatedAt = DateTime.UtcNow
                });
            }


            await _uow.SaveChangesAsync();

            return BaseResult<PaymentResponseDto>.Ok(new PaymentResponseDto
            {
                TransactionId = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                PaidAt = payment.PaidAt
            });
        }

        public async Task<BaseResult<PaymentResponseDto>> GetByOrderAsync(
            Guid userId, Guid orderId)
        {
            var order = await _uow.Order.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<PaymentResponseDto>.NotFound();

            var payment = await _uow.PaymentTransaction.GetByOrderIdAsync(orderId);
            if (payment == null)
                return BaseResult<PaymentResponseDto>.NotFound("Chưa có thanh toán");

            return BaseResult<PaymentResponseDto>.Ok(new PaymentResponseDto
            {
                TransactionId = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                PaidAt = payment.PaidAt
            });
        }
    }
}
