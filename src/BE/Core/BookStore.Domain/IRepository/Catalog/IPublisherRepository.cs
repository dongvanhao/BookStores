using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IPublisherRepository : IGenericRepository<Entities.Catalog.Publisher>
    {
        Task<bool> ExitsByNameAsync(string name);
    }
}
