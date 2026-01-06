using BookStore.Application.Dtos.Pricing_Inventory.Warehouse;
using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Pricing_Inventory
{
    public static class WarehouseMapping
    {
        public static WarehouseResponseDto ToDto(this Warehouse w)
        {
            return new WarehouseResponseDto
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                Description = w.Description
            };
        }

        public static WarehouseDetailDto ToDetailDto(this Warehouse w)
        {
            return new WarehouseDetailDto
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                Description = w.Description,
                StockItems = w.StockItems.Select(s => s.ToDto()).ToList()
            };
        }
    }
}
