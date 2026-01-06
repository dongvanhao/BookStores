using BookStore.Application.Dtos.Pricing_Inventory;
using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Pricing_Inventory
{
    public static class CouponMapping
    {
        public static CouponDto ToDto(this Coupon c)
        {
            return new CouponDto
            {
                Id = c.Id,
                Code = c.Code,
                Value = c.Value,
                IsPercentage = c.IsPercentage,
                Expiration = c.Expiration
            };
        }
    }
}
