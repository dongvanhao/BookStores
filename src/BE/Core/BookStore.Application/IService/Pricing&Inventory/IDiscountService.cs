using BookStore.Application.Dtos.Pricing_Inventory.Discount;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface IDiscountService
    {
        Task<BaseResult<IReadOnlyList<DiscountDto>>> GetActiveAsync();
        Task<BaseResult<Guid>> CreateAsync(CreateDiscountDto dto);
        Task<BaseResult<bool>> ToggleAsync(Guid id);
    }
}
