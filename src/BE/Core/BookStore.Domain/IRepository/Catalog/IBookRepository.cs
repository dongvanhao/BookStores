using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Catalog
{
    public interface IBookRepository 
    {
        Task<Book?> GetByIdAsync(Guid id);
        Task<Book?> GetDetailAsync(Guid id);
        Task AddAsync (Book book);
        Task DeleteAsync (Book book);
    }
}
