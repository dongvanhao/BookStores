using BookStore.Application.Dtos.Pricing_Inventory.StockItem;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface IStockItemService
    {
        Task<BaseResult<StockItemResponseDto>> GetAsync(Guid bookId, Guid warehouseId);
        Task<BaseResult<bool>> IncreaseAsync(AdjustStockRequestDto dto);
        Task<BaseResult<bool>> DecreaseAsync(AdjustStockRequestDto dto);
    }
}
