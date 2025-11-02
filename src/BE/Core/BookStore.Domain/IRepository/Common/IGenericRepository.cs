using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Common
{
    public interface IGenericRepository<T> where T : class
    {
        Task <T?> GetByIdAsync(Guid id);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);//Lấy thực thể đầu tiên thỏa mãn một điều kiện (predicate).

        /// <summary>
        /// Lấy một danh sách chỉ đọc các thực thể, hỗ trợ lọc, sắp xếp và phân trang.
        /// Đây là phương thức truy vấn chính, mạnh mẽ nhất.
        /// </summary>
        /// <param name="predicate">Điều kiện lọc (mệnh đề WHERE). Null nếu lấy tất cả.</param>
        /// <param name="orderBy">Hàm sắp xếp (mệnh đề ORDER BY). Null nếu không sắp xếp.</param>
        /// <param name="skip">Số lượng bản ghi cần bỏ qua (dùng cho phân trang).</param>
        /// <param name="take">Số lượng bản ghi cần lấy (dùng cho phân trang).</param>
        /// <returns>Một danh sách chỉ đọc các thực thể thỏa mãn.</returns>
        Task<IReadOnlyList<T>> GetListAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? skip = null,
            int? take = null
        );
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null); /// Đếm số lượng thực thể thỏa mãn một điều kiện.
                                                                           /// Thường dùng để tính tổng số trang (TotalCount) cho PagedResult.
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
