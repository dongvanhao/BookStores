using BookStore.Domain.Entities.Pricing_Inventory;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Pricing___Inventory
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly AppDbContext _context;

        public WarehouseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Warehouse?> GetByIdAsync(Guid id)
        {
            return await _context.Warehouses
                .Include(w => w.StockItems)
                .ThenInclude(si => si.Book)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task AddAsync(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
        }

        public void Update(Warehouse warehouse)
        {
            warehouse.UpdatedAt = DateTime.UtcNow;
            _context.Warehouses.Update(warehouse);
        }
    }
}
