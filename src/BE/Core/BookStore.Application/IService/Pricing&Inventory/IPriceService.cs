using BookStore.Application.Dtos.Pricing_Inventory.Price;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface IPriceService
    {
        Task<BaseResult<PriceResponseDto>> CreateAsync(CreatePriceRequestDto dto);
        Task<BaseResult<PriceResponseDto>> GetCurrentAsync(Guid bookId);
        Task<BaseResult<IReadOnlyList<PriceResponseDto>>> GetHistoryAsync(Guid bookId);
    }
}
