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
            code: "Auth.EmailExists",
            message: "Email đã tồn tại",
            type: ErrorType.Conflict // 409 Conflict
        );

        public static readonly Error InvalidCredentials = new(
            code: "Auth.InvalidCredentials",
            message: "Email hoặc password không đúng",
            type: ErrorType.Unauthorized // 401 Unauthorized
        );

        // Bạn có thể thêm các lỗi khác ở đây...
        public static readonly Error ForbiddenAccess = new(
            code: "Auth.Forbidden",
            message: "Bạn không có quyền thực hiện hành động này",
            type: ErrorType.Forbidden // 403 Forbidden
        );
    }
}
