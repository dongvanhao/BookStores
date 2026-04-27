namespace BookStore.Shared.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items       { get; } //danh sách dữ liệu hiện tại
    public int              TotalCount  { get; } //tổng số item
    public int              PageNumber  { get; } //trang hiện tại
    public int              PageSize    { get; } //số item mỗi trang

    // Tính toán tự động không để client tự tính
    public int  TotalPages   => (int)Math.Ceiling(TotalCount / (double)PageSize); //ví dụ: 97/10 = 9.7 -> làm tròn = 10
    public bool HasNextPage  => PageNumber < TotalPages; //có trang tiếp theo không
    public bool HasPrevPage  => PageNumber > 1; //có trang trước đó không

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) //constructor
    {
        Items      = items;//list dữ liệu
        TotalCount = totalCount;//tổng số item
        PageNumber = pageNumber;//trang hiện tại
        PageSize   = pageSize;//số item mỗi trang
    }

    // Factory method cho gọn — dùng khi đã có List sẵn
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, PaginationParams @params)
        => new(items, totalCount, @params.PageNumber, @params.PageSize);
}
