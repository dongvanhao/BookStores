using BookStore.Application.Dtos.Pricing_Inventory;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.Mappers.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Pricing_Inventory
{
    public class CouponService : ICouponService
    {
        private readonly IUnitOfWork _uow;

        public CouponService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<CouponDto>>> GetMyCouponsAsync(Guid userId)
        {
            var list = await _uow.Coupon.GetAvailableByUserAsync(userId);
            return BaseResult<IReadOnlyList<CouponDto>>.Ok(
                list.Select(c => c.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<decimal>> ApplyAsync(
            Guid userId, ApplyCouponRequestDto dto)
        {
            var order = await _uow.Order.GetByIdAsync(dto.OrderId);
            if (order == null || order.UserId != userId)
                return BaseResult<decimal>.NotFound("Không tìm thấy đơn hàng");

            if (order.CouponId.HasValue)
                return BaseResult<decimal>.Fail(
                    "Coupon.Used",
                    "Đơn hàng đã áp dụng coupon",
                    ErrorType.Conflict
                );

            var coupon = await _uow.Coupon.GetValidByCodeAsync(dto.Code, userId);
            if (coupon == null)
                return BaseResult<decimal>.Fail(
                    "Coupon.Invalid",
                    "Coupon không hợp lệ",
                    ErrorType.Validation
                );

            decimal discount = coupon.IsPercentage
                ? order.TotalAmount * coupon.Value / 100
                : coupon.Value;

            order.DiscountAmount = discount;
            order.CouponId = coupon.Id;
            coupon.IsUsed = true;

            _uow.Order.Update(order);
            _uow.Coupon.Update(coupon);

            await _uow.SaveChangesAsync();

            return BaseResult<decimal>.Ok(discount);
        }

        public async Task<BaseResult<Guid>> CreateAsync(CreateCouponDto dto)
        {
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Value = dto.Value,
                IsPercentage = dto.IsPercentage,
                Expiration = dto.Expiration,
                UserId = dto.UserId
            };

            await _uow.Coupon.AddAsync(coupon);
            await _uow.SaveChangesAsync();

            return BaseResult<Guid>.Ok(coupon.Id);
        }
    }
}
