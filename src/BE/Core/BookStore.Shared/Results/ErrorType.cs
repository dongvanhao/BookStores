namespace BookStore.Shared.Results;

public enum ErrorType
{
    None            = 0,              // Thành công, không có lỗi
    Validation      = 1,              // Lỗi dữ liệu đầu vào (400)
    NotFound        = 2,              // Không tìm thấy (404)
    Conflict        = 3,              // Trùng lặp (409)
    Unauthorized    = 4,              // Chưa đăng nhập (401)
    Forbidden       = 5,              // Không có quyền (403)
    Failure         = 6,              // Lỗi hệ thống (500)
    Unexpected      = 7               // Lỗi không mong muốn
}