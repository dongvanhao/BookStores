using BookStore.Application.Dtos.Ordering_Payment.Order;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.Mappers.Ordering_Payment;
using BookStore.Domain.Entities.Ordering;
using BookStore.Shared.Common;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Domain.IRepository.Ordering_Payment;

namespace BookStore.Application.Services.Ordering_Payment
{
    public class OrderService : IOrderService
    {
        private readonly IUserAddressRepository _userAddresses;
        private readonly ICartRepository _carts;
        private readonly IOrderRepository _orders;
        private readonly IOrderStatusLogRepository _orderStatusLogs;
        private readonly IDbSession _session;

        public OrderService(
            IUserAddressRepository userAddresses,
            ICartRepository carts,
            IOrderRepository orders,
            IOrderStatusLogRepository orderStatusLogs,
            IDbSession session)
        {
            _userAddresses = userAddresses;
            _carts = carts;
            _orders = orders;
            _orderStatusLogs = orderStatusLogs;
            _session = session;
        }

        public async Task<BaseResult<IReadOnlyList<OrderSummaryDto>>> GetMyOrderAsync(Guid userId)
        {
            var orders = await _orders.GetByUserAsync(userId);
            return BaseResult<IReadOnlyList<OrderSummaryDto>>.Ok(
                orders.Select(o => o.ToSummary()).ToList());
        }

        public async Task<BaseResult<OrderDetailDto>> GetDetailAsync(Guid userId, Guid orderId)
        {
            var order = await _orders.GetDetailAsync(orderId, userId);
            if (order == null)
                return BaseResult<OrderDetailDto>.NotFound("Order not found.");

            return BaseResult<OrderDetailDto>.Ok(order.ToDetail());
        }

        public async Task<BaseResult<bool>> CancelAsync(Guid userId, Guid orderId)
        {
            var order = await _orders.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return BaseResult<bool>.NotFound();

            if (order.Status != OrderStatus.Pending)
                return BaseResult<bool>.Fail(
                    "Order.CannotCancel",
                    "Order can only be cancelled when status is Pending.",
                    ErrorType.Conflict);

            var oldStatus = order.Status;
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            _orders.Update(order);

            await _orderStatusLogs.AddAsync(new OrderStatusLog
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.Cancelled,
                ChangedAt = DateTime.UtcNow
            });

            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<CheckoutResponseDto>> CheckoutAsync(Guid userId, CheckoutRequestDto request)
        {
            var cart = await _carts.GetActiveByUserAsync(userId);
            if (cart == null || !cart.Items.Any())
                return BaseResult<CheckoutResponseDto>.Fail(
                    "Checkout.EmptyCart",
                    "Cart is empty.",
                    ErrorType.Validation);

            var userAddress = await _userAddresses.GetByIdAsync(request.AddressId);
            if (userAddress == null || userAddress.UserId != userId)
                return BaseResult<CheckoutResponseDto>.Fail(
                    "Checkout.InvalidAddress",
                    "Invalid delivery address.",
                    ErrorType.Validation);

            var totalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatus.Pending,
                OrderNumber = GenerateOrderNumber(),
                SubTotal = totalAmount,
                DiscountAmount = 0,
            };

            order.ShippingAddress = new OrderAddress
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                RecipientName = userAddress.RecipientName,
                PhoneNumber = userAddress.PhoneNumber,
                Province = userAddress.Province,
                District = userAddress.District,
                Ward = userAddress.Ward,
                Street = userAddress.StreetAddress
            };

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

            _carts.Delete(cart);

            await _orders.AddAsync(order);

            await _orderStatusLogs.AddAsync(new OrderStatusLog
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OldStatus = OrderStatus.Pending,
                NewStatus = OrderStatus.Pending,
                ChangedAt = DateTime.UtcNow
            });

            await _session.SaveChangesAsync();

            return BaseResult<CheckoutResponseDto>.Ok(new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                FinalAmount = order.FinalAmount
            });
        }

        private static string GenerateOrderNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"BK-{timestamp}-{suffix}";
        }
    }
}
