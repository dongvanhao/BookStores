using BookStore.Application.Dtos.Pricing_Inventory;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface ICouponService
    {
        Task<BaseResult<IReadOnlyList<CouponDto>>> GetMyCouponsAsync(Guid userId);
        Task<BaseResult<decimal>> ApplyAsync(Guid userId, ApplyCouponRequestDto dto);
        Task<BaseResult<Guid>> CreateAsync(CreateCouponDto dto);
    }
}
