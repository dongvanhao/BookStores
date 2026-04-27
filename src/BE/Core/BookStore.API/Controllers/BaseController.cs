using BookStore.Shared.Responses;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        // Dùng cho GET single / PUT / DELETE
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(ApiResponse<T>.Ok(result.Value));

            return MapError<T>(result.Error);
        }

        // Dùng cho GET list có paging
        protected IActionResult HandlePagedResult<T>(Result<PagedResult<T>> result)
        {
            if (result.IsSuccess)
                return Ok(ApiResponse<PagedResult<T>>.Ok(result.Value));

            return MapError<PagedResult<T>>(result.Error);
        }

        // Dùng cho POST — trả về 201 Created kèm Location header
        protected IActionResult HandleCreated<T>(Result<T> result, string actionName)
        {
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    actionName,
                    new { id = result.Value },
                    ApiResponse<T>.Ok(result.Value, "Resource created successfully.")
                );
            }

            return MapError<T>(result.Error);
        }

        // Dùng cho các action không có data trả về (VD: Delete)
        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(ApiResponse.Ok());

            return MapError(result.Error);
        }

        // Map ErrorType → HTTP status — tập trung 1 chỗ duy nhất
        private IActionResult MapError<T>(Error error)
        {
            var response = ApiResponse<T>.Fail(error.Description, error.Code);

            return error.Type switch
            {
                ErrorType.NotFound => NotFound(response),
                ErrorType.Validation => BadRequest(response),
                ErrorType.Conflict => Conflict(response),
                ErrorType.Unauthorized => Unauthorized(response),
                _ => StatusCode(StatusCodes.Status500InternalServerError, response)
            };
        }

        private IActionResult MapError(Error error)
        {
            var response = ApiResponse.Fail(error.Description, error.Code);

            return error.Type switch
            {
                ErrorType.NotFound => NotFound(response),
                ErrorType.Validation => BadRequest(response),
                ErrorType.Conflict => Conflict(response),
                ErrorType.Unauthorized => Unauthorized(response),
                _ => StatusCode(StatusCodes.Status500InternalServerError, response)
            };
        }
    }
}
