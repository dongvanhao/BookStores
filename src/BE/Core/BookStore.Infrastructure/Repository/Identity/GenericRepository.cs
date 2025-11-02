using BookStore.Domain.IRepository.Common;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Identity
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
            {
                return await _dbSet.CountAsync();
            }
            return await _dbSet.Where(predicate).CountAsync();
        }
        public void Delete(T entity)
        {
            // EF Core chỉ cần đánh dấu là Deleted, Unit of Work sẽ lo việc Save
            _dbSet.Remove(entity);
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<IReadOnlyList<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? skip = null,
            int? take = null)
        {
            IQueryable<T> query = _dbSet;

            // 1. Lọc (Filter)
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 2. Sắp xếp (Order)
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 3. Phân trang (Paging)
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            // AsNoTracking() tốt cho hiệu suất khi chỉ đọc
            return await query.AsNoTracking().ToListAsync();
        }

        public void Update(T entity)
        {
            // Đính kèm entity và đánh dấu là Modified
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
}
