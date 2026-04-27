namespace BookStore.Shared.Common;

public class QueryParams : PaginationParams // kế thừa PaginationParams
{
    public string? SearchTerm    { get; set; }              // tìm kiếm tổng quát
    public string? SortBy        { get; set; }              // tên field cần sort
    public bool    IsAscending   { get; set; } = true;     // sắp xếp tăng/giảm (mặc định tăng) 
}

#region Ví dụ về QueryParams trong API
/*
// Client gửi lên:
// GET /api/books?PageNumber=2&PageSize=10&SortBy=Title&IsAscending=true&SearchTerm=asp.net
// GET /api/books?PageNumber=1&PageSize=20&SortBy=Price&IsAscending=false

// Tạo class query riêng cho từng module, class này kế thừa QueryParams
// BookStore.Application/Books/Queries/GetBooksQuery.cs
public sealed class GetBooksQuery : QueryParams
{
    public Guid?    CategoryId { get; set; }   // filter riêng của Books
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
    public bool?    IsAvailable { get; set; }
}
*/
#endregion