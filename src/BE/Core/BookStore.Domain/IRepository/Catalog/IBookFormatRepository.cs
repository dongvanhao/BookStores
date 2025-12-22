using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookFormatRepository : IGenericRepository<Entities.Catalog.BookFormat>
    {
        Task<bool> ExistsByTypeAsync(string formatType);
    }
}
