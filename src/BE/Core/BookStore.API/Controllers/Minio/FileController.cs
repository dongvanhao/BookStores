using BookStore.Application.IService.Storage;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.Minio
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : BaseController 
    {
        private readonly IStorageService _storage;

        public FileController(IStorageService storage)
        {
            _storage = storage;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")] //để Swagger hiểu đây là form-data
        public async Task<IActionResult> Upload(IFormFile file, string? folder = null)
        {
            if (file == null || file.Length == 0)
                return FromResult<string>(BaseResult<string>.Fail(
                    code: "File.Empty",
                    message: "File không hợp lệ",
                    type: ErrorType.Validation));

            var result = await _storage.UploadAsync(
                stream: file.OpenReadStream(),
                size: file.Length,
                contextType: file.ContentType,
                fileName: file.FileName,
                folder: folder
                );

            return FromResult(result);
        }

        [HttpGet("{objectKey}")]
        public async Task<IActionResult> Download(string objectKey)
        {
            var result = await _storage.DownloadAsync(objectKey);

            return result.Match<IActionResult>(
                onSuccess: stream => File(stream, "application/octet-stream", objectKey),
                onFailure: error => CreateErrorResponse(error) // từ BaseController
            );
        }

        [HttpDelete("{objectKey}")]
        public async Task<IActionResult> Delete(string objectKey)
        {
            var result = await _storage.DeleteAsync(objectKey);
            return FromResult(result);
        }

        [HttpPut("{objectKey}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(string objectKey, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return FromResult<string>(BaseResult<string>.Fail(
                    code: "File.Empty",
                    message: "File không hợp lệ",
                    type: ErrorType.Validation));

            var result = await _storage.UpdateAsync(
                objectKey: objectKey,
                stream: file.OpenReadStream(),
                size: file.Length,
                contentType: file.ContentType,
                fileName: file.FileName
            );

            return FromResult(result);
        }

    }
}
