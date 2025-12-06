using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Shared.Common
{
    /// <summary>
    /// Đối tượng Lỗi có cấu trúc, dùng "record" để đảm bảo
    /// tính bất biến (immutable) sau khi được tạo ra.
    /// </summary>
    public record Error(
            string Code,
            string Message,
            ErrorType Type = ErrorType.Failure
         );
}
