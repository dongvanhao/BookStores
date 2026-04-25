using BookStore.Application.Dtos.Ordering_Payment.Payment;
using BookStore.Application.Dtos.Ordering_Payment.PaymentRequestDto;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Domain.Entities.Ordering;
using BookStore.Shared.Common;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Ordering_Payment;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderStatusLogRepository _orderStatusLogs;
        private readonly IPaymentTransactionRepository _paymentTransactions;
        private readonly IDbSession _session;

        public PaymentService(
            IOrderRepository orders,
            IOrderStatusLogRepository orderStatusLogs,
            IPaymentTransactionRepository paymentTransactions,
            IDbSession session)
        {
            _orders = orders;
            _orderStatusLogs = orderStatusLogs;
            _paymentTransactions = paymentTransactions;
            _session = session;
        }

        public async Task<BaseResult<PaymentResponseDto>> PayAsync(Guid userId, PaymentRequestDto request)
        {
            var order = await _orders.GetByIdAsync(request.OrderId);
            if (order == null || order.UserId != userId)
                return BaseResult<PaymentResponseDto>.NotFound("Order not found.");

            if (order.Status != OrderStatus.Pending)
                return BaseResult<PaymentResponseDto>.Fail(
                    "Payment.InvalidOrder",
                    "Order cannot be paid in its current status.",
                    ErrorType.Conflict);

            var existing = await _paymentTransactions.GetByOrderIdAsync(order.Id);
            if (existing != null)
                return BaseResult<PaymentResponseDto>.Fail(
                    "Payment.Existed",
                    "A payment transaction already exists for this order.",
                    ErrorType.Conflict);

            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = "Sandbox",
                PaymentMethod = "Online",
                TransactionCode = $"SBX-{Guid.NewGuid():N}",
                Amount = order.FinalAmount,
                Status = request.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failed,
                PaidAt = request.IsSuccess ? DateTime.UtcNow : null
            };

            await _paymentTransactions.AddAsync(payment);

            if (request.IsSuccess)
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
                _orders.Update(order);

                await _orderStatusLogs.AddAsync(new OrderStatusLog
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = OrderStatus.Paid,
                    ChangedAt = DateTime.UtcNow
                });
            }

            await _session.SaveChangesAsync();

            return BaseResult<PaymentResponseDto>.Ok(new PaymentResponseDto
            {
                TransactionId = payment.Id,
                Status = payment.Status.ToString(),
                Amount = payment.Amount,
                PaidAt = payment.PaidAt
            });
        }

        public async Task<BaseResult<PaymentResponseDto>> GetByOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _orders.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<PaymentResponseDto>.NotFound();

            var payment = await _paymentTransactions.GetByOrderIdAsync(orderId);
            if (payment == null)
                return BaseResult<PaymentResponseDto>.NotFound("No payment found for this order.");

            return BaseResult<PaymentResponseDto>.Ok(new PaymentResponseDto
            {
                TransactionId = payment.Id,
                Status = payment.Status.ToString(),
                Amount = payment.Amount,
                PaidAt = payment.PaidAt
            });
        }
    }
}
