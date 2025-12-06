// File: BookStore.API.Controllers/BaseController.cs
using BookStore.Shared.Common;
using BookStore.Shared.Errors; // Thêm
using Microsoft.AspNetCore.Mvc;
using System.Net; // Thêm

namespace BookStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Chuyển BaseResult<T> sang IActionResult chuẩn,
        /// tự động ánh xạ ErrorType sang HTTP Status Code.
        /// </summary>
        protected IActionResult FromResult<T>(BaseResult<T> result)
        {
            if (result == null)
            {
                // Lỗi 500 nếu service trả về null
                return CreateErrorResponse(CommonErrors.DefaultError);
            }

            if (result.IsSuccess)
            {
                // 200 OK
                // Trả về thẳng giá trị, không cần bọc thêm
                return Ok(result.Value);
            }

            // Xử lý lỗi (result.IsSuccess == false)
            var error = result.Error ?? CommonErrors.DefaultError;
            return CreateErrorResponse(error);
        }

        /// <summary>
        /// Helper private để tạo response lỗi từ Error
        /// </summary>
        protected IActionResult CreateErrorResponse(Error error)
        {
            // Ánh xạ ErrorType sang HttpStatusCode
            var statusCode = error.Type switch
            {
                ErrorType.Validation => HttpStatusCode.BadRequest,       // 400
                ErrorType.Unauthorized => HttpStatusCode.Unauthorized, // 401
                ErrorType.Forbidden => HttpStatusCode.Forbidden,         // 403
                ErrorType.NotFound => HttpStatusCode.NotFound,           // 404
                ErrorType.Conflict => HttpStatusCode.Conflict,         // 409
                ErrorType.Internal => HttpStatusCode.InternalServerError, // 500
                ErrorType.Failure => HttpStatusCode.InternalServerError,  // 500
                _ => HttpStatusCode.InternalServerError
            };

            // Trả về ProblemDetails (chuẩn của .NET Core)
            // Nó sẽ tự động format lỗi cho client
            return Problem(
                title: error.Code,
                detail: error.Message,
                statusCode: (int)statusCode
            );
        }
    }
}