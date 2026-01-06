using BookStore.Application.Dtos.Pricing_Inventory.Discount;
using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Pricing_Inventory
{
    public static class DiscountMapping
    {
        public static DiscountDto ToDto(this Discount d)
        {
            return new DiscountDto
            {
                Id = d.Id,
                Code = d.Code,
                Title = d.Title,
                Description = d.Description,
                Percentage = d.Percentage,
                MaxDiscountAmount = d.MaxDiscountAmount,
                EndDate = d.EndDate
            };
        }
    }
}
