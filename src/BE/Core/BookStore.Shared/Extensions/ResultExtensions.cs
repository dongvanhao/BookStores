using BookStore.Shared.Responses;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Shared.Extensions;

public static class ResultExtensions
{
    //Chuyển Result sang ApiResponse
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(ApiResponse<T>.Ok(result.Value));

        return result.Error.Type switch
        {
            ErrorType.NotFound     => new NotFoundObjectResult(
                                         ApiResponse<T>.Fail(result.Error.Description, result.Error.Code)),
            ErrorType.Validation   => new BadRequestObjectResult(
                                         ApiResponse<T>.Fail(result.Error.Description, result.Error.Code)),
            ErrorType.Conflict     => new ConflictObjectResult(
                                         ApiResponse<T>.Fail(result.Error.Description, result.Error.Code)),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(
                                         ApiResponse<T>.Fail(result.Error.Description, result.Error.Code)),
            _                      => new ObjectResult(ApiResponse<T>.Fail(result.Error.Description, result.Error.Code))
                                         { StatusCode = 500 }
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(ApiResponse.Ok());

        return result.Error.Type switch
        {
            ErrorType.NotFound   => new NotFoundObjectResult(
                                        ApiResponse.Fail(result.Error.Description, result.Error.Code)),
            ErrorType.Validation => new BadRequestObjectResult(
                                        ApiResponse.Fail(result.Error.Description, result.Error.Code)),
            _                    => new ObjectResult(
                                        ApiResponse.Fail(result.Error.Description, result.Error.Code))
                                        { StatusCode = 500 }
        };
    }
}

#region Tại sao dùng ToActionResult()
/*
// Cách 1: Controller trả thẳng về Result<Book>
public async Task<Result<Book>> GetAsync(Guid id)
{
    return await _service.GetByIdAsync(id);  // Implicit cast
}

// Cách 2: Controller tự xử lý mapping
public async Task<IActionResult> GetAsync(Guid id)
{
    var result = await _service.GetByIdAsync(id);

    if (result.IsSuccess)
        return Ok(ApiResponse<Book>.Ok(result.Value));

    return result.Error.Type switch
    {
        ErrorType.NotFound   => NotFound(ApiResponse<Book>.Fail("Không tìm thấy")),   // 404
        ErrorType.Validation => BadRequest(ApiResponse<Book>.Fail("Sai dữ liệu")), // 400
        _                    => StatusCode(500, ApiResponse<Book>.Fail("Lỗi hệ thống"))
    };
}

// Cách 3 (Khuyên dùng): Dùng Extension Method
public async Task<IActionResult> GetAsync(Guid id)
{
    var result = await _service.GetByIdAsync(id);
    return result.ToActionResult();  // Tự động xử lý tất cả
}

*/
#endregion
