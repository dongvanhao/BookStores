using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Shared.Common
{
    public static class CommonErrors
    {
        public static readonly Error DefaultError = new(
            "Error.Default", "Đã có lỗi xảy ra.", ErrorType.Internal);

        public static readonly Error InvalidRequest = new(
            "Common.InvalidRequest", "Yêu cầu không hợp lệ.", ErrorType.Validation);

        public static readonly Error UserNotFound = new(
            "User.NotFound", "Không tìm thấy người dùng.", ErrorType.NotFound);

        public static Error InternalServerError(string message = "Lỗi hệ thống.") => new(
             "Internal.Exception", message, ErrorType.Internal);
    }
}
