using BookStore.Application.Dtos.Pricing_Inventory.Warehouse;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Pricing_Inventory
{
    public interface IWarehouseService
    {
        Task<BaseResult<IReadOnlyList<WarehouseResponseDto>>> GetAllAsync();
        Task<BaseResult<WarehouseDetailDto>> GetByIdAsync(Guid id);
        Task<BaseResult<WarehouseResponseDto>> CreateAsync(WarehouseRequestDto dto);
        Task<BaseResult<bool>> UpdateAsync(Guid id, WarehouseRequestDto dto);
    }
}
