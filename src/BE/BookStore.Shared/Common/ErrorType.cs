using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Shared.Common
{
    /// <summary>
    /// Phân loại lỗi để giúp Controller ánh xạ
    /// sang HTTP Status Code một cách nhất quán.
    /// </summary>
    public enum ErrorType
    {
        Failure, //Lỗi chung (-> 500 Server Error)
        NotFound,// Not Found Error (-> 404 Error)
        Validation,// Validation Error (-> 400 Error)
        Unauthorized,// Unauthorized Error(-> 401 Error)
        Forbidden, // Forbidden Error(-> 403 Error)
        Conflict, // Conflict Error(-> 409 Error)
        Internal // Server Error(-> 500 Error)
    }
}
