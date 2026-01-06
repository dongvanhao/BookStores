using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.Entities.Ordering;
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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        public OrderService(IUnitOfWork uow) { _uow = uow; }

        public async Task<BaseResult<IReadOnlyList<OrderSummaryDto>>> GetMyOrderAsync(Guid userId)
        {
            var orders = await _uow.Order.GetByUserAsync(userId);
            return BaseResult<IReadOnlyList<OrderSummaryDto>>.Ok(
                orders.Select(o => o.ToSummary()).ToList()
            );
        }

        public async Task<BaseResult<OrderDetailDto>> GetDetailAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Order.GetDetailAsync(orderId, userId);
            if (order == null)
                return BaseResult<OrderDetailDto>.NotFound("Không tìm thấy đơn hàng");

            return BaseResult<OrderDetailDto>.Ok(order.ToDetail());
        }

        public async Task<BaseResult<bool>> CancelAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Order.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<bool>.NotFound();

            if (order.Status != "Pending")
                return BaseResult<bool>.Fail(
                    "Order.CannotCancel",
                    "Chỉ được hủy đơn khi chưa thanh toán",
                    ErrorType.Conflict
                );

            var oldStatus = order.Status;

            order.Status = "Cancelled";
            order.CancelledAt = DateTime.UtcNow;
            _uow.Order.Update(order);

            // ✅ OrderStatusLog
            await _uow.OrderStatusLog.AddAsync(new OrderStatusLog
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = "Cancelled",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "User"
            });

            // ✅ OrderHistory
            await _uow.OrderHistory.AddAsync(new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Action = "CancelOrder",
                Details = "User hủy đơn hàng khi chưa thanh toán",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync();


            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<CheckoutResponseDto>> CheckoutAsync(Guid userId, CheckoutRequestDto request)
        {
            // 1️⃣ Lấy cart active
            var cart = await _uow.Cart.GetActiveByUserAsync(userId);
            if (cart == null || !cart.Items.Any())
                return BaseResult<CheckoutResponseDto>.Fail(
                    "Checkout.EmptyCart",
                    "Giỏ hàng trống",
                    ErrorType.Validation
                );
            var userAddress = await _uow.UserAddresses.GetByIdAsync(request.AddressId);
            if (userAddress == null || userAddress.UserId != userId)
                return BaseResult<CheckoutResponseDto>.Fail(
                    "Checkout.InvalidAddress",
                    "Địa chỉ không hợp lệ",
                    ErrorType.Validation
                );
            // 2️⃣ Tính tổng tiền
            var totalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

            // 3️⃣ Áp coupon (sandbox – chưa xử lý thật)
            decimal discount = 0;
            if (request.CouponId.HasValue)
            {
                // TODO: validate coupon
                discount = 0;
            }

            // 4️⃣ Tạo Order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Pending",
                OrderNumber = GenerateOrderNumber(),
                TotalAmount = totalAmount,
                DiscountAmount = discount,
            };
            var orderAddress = new OrderAddress
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,

                RecipientName = userAddress.ReipientName,
                PhoneNumber = userAddress.PhoneNumber,
                Province = userAddress.Povince,
                District = userAddress.District,
                Ward = userAddress.Ward,
                Street = userAddress.StreetAddress
            };

            // 🔗 GẮN VÀO ORDER (quan trọng)
            order.OrderAddress = orderAddress;

            // 5️⃣ Tạo OrderItem từ CartItem
            foreach (var cartItem in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    BookId = cartItem.BookId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice
                });
            }

            // 6️⃣ Đóng cart
            cart.IsActive = false;
            _uow.Cart.Update(cart);

            // 7️⃣ Persist
            await _uow.Order.AddAsync(order);
            // ✅ OrderStatusLog: null → Pending
            await _uow.OrderStatusLog.AddAsync(new OrderStatusLog
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = "None",
                NewStatus = "Pending",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "User"
            });

            // ✅ OrderHistory: Checkout
            await _uow.OrderHistory.AddAsync(new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Action = "Checkout",
                Details = $"User checkout đơn hàng với tổng tiền {order.FinalAmount:N0}",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync();

            return BaseResult<CheckoutResponseDto>.Ok(new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                FinalAmount = order.FinalAmount
            });
        }
        private string GenerateOrderNumber()
        {
            return $"BK-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

    }

}
