using BookStore.Domain.IRepository.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Common
{
    public interface IUnitOfWork : IDisposable
    {
        // Thêm các repository cụ thể của bạn ở đây
        // Ví dụ: IBookRepository BookRepository { get; }
        // Ví dụ: IShipperRepository ShipperRepository { get; }
        IBookRepository BookRepository { get; }
        Task<int> SaveChangesAsync();// Phương thức để commit tất cả thay đổi
    }
}
