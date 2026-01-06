using BookStore.Domain.Entities.Pricing_Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Pricing___Inventory
{
    public interface IWarehouseRepository
    {
        Task<IReadOnlyList<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(Guid id);
        Task AddAsync(Warehouse warehouse);
        void Update(Warehouse warehouse);
    }
}
