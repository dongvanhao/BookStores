using BookStore.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace BookStore.Shared.Extensions;

//Dùng chung cho mọi repository, không cần lặp lại .Skip() .Take() ở khắp nơi
public static class QueryableExtensions
{
    //Apply skip/take
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PaginationParams @params)
        => query
            .Skip((@params.PageNumber - 1) * @params.PageSize)
            .Take(@params.PageSize);

    //Apply sort động theo tên field - dùng System.linq.Dynamic hoặc switch
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return query;

        //dùng package System.Linq.Dynamic.Core
        var direction = isAscending ? "ascending" : "descending";
        return query.OrderBy($"{sortBy} {direction}");
    }

    //gộp cả paging + sort + trả về PagedResult trong 1 lần gọi DB
    public static async Task<BookStore.Shared.Common.PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams @params,
        CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct); // query 1: đếm tổng

        var items = await query
            .ApplyPaging(@params)
            .ToListAsync(ct); // query 2: lấy dữ liệu

        return BookStore.Shared.Common.PagedResult<T>.Create(items, totalCount, @params);
    }
}
