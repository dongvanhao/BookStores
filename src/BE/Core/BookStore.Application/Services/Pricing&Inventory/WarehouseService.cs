using BookStore.Application.Dtos.Pricing_Inventory.Warehouse;
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
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _uow;

        public WarehouseService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<WarehouseResponseDto>>> GetAllAsync()
        {
            var list = await _uow.Warehouse.GetAllAsync();
            return BaseResult<IReadOnlyList<WarehouseResponseDto>>.Ok(
                list.Select(w => w.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<WarehouseDetailDto>> GetByIdAsync(Guid id)
        {
            var warehouse = await _uow.Warehouse.GetByIdAsync(id);
            if (warehouse == null)
                return BaseResult<WarehouseDetailDto>.NotFound("Không tìm thấy kho");

            return BaseResult<WarehouseDetailDto>.Ok(warehouse.ToDetailDto());
        }

        public async Task<BaseResult<WarehouseResponseDto>> CreateAsync(
            WarehouseRequestDto dto)
        {
            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Address = dto.Address,
                Description = dto.Description
            };

            await _uow.Warehouse.AddAsync(warehouse);
            await _uow.SaveChangesAsync();

            return BaseResult<WarehouseResponseDto>.Ok(warehouse.ToDto());
        }

        public async Task<BaseResult<bool>> UpdateAsync(
            Guid id, WarehouseRequestDto dto)
        {
            var warehouse = await _uow.Warehouse.GetByIdAsync(id);
            if (warehouse == null)
                return BaseResult<bool>.NotFound();

            warehouse.Name = dto.Name;
            warehouse.Address = dto.Address;
            warehouse.Description = dto.Description;

            _uow.Warehouse.Update(warehouse);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }
    }
}
