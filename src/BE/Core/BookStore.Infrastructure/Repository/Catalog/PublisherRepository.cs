using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Catalog
{
    public class PublisherRepository : IPublisherRepository
    {
        public Task AddAsync(Publisher entity)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync(Expression<Func<Publisher, bool>>? predicate = null)
        {
            throw new NotImplementedException();
        }

        public void Delete(Publisher entity)
        {
            throw new NotImplementedException();
        }

        public Task<Publisher?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Publisher?> GetFirstOrDefaultAsync(Expression<Func<Publisher, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Publisher>> GetListAsync(Expression<Func<Publisher, bool>>? predicate = null, Func<IQueryable<Publisher>, IOrderedQueryable<Publisher>>? orderBy = null, int? skip = null, int? take = null)
        {
            throw new NotImplementedException();
        }

        public void Update(Publisher entity)
        {
            throw new NotImplementedException();
        }
    }
}
