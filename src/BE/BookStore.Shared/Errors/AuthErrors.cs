using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Shared.Errors
{
    /// <summary>
    /// Định nghĩa các lỗi liên quan đến Authentication và Authorization.
    /// Sử dụng record 'Error' để nhất quán với BaseResult.
    /// </summary>
    public static class AuthErrors
    {
        public static readonly Error EmailExists = new(
            Code: "Auth.EmailExists",
            Message: "Email đã tồn tại",
            Type: ErrorType.Conflict // 409 Conflict
        );

        public static readonly Error InvalidCredentials = new(
            Code: "Auth.InvalidCredentials",
            Message: "Email hoặc password không đúng",
            Type: ErrorType.Unauthorized // 401 Unauthorized
        );

        // Bạn có thể thêm các lỗi khác ở đây...
        public static readonly Error ForbiddenAccess = new(
            Code: "Auth.Forbidden",
            Message: "Bạn không có quyền thực hiện hành động này",
            Type: ErrorType.Forbidden // 403 Forbidden
        );
    }
}
