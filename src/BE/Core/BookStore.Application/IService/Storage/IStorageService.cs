using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Storage
{
    public interface IStorageService
    {
        //Upload file lên storage và trả về objectKey (đường dẫn lưu trong MinIO).
        Task<BaseResult<string>> UploadAsync(
            Stream stream,
            long size,
            string contextType,
            string fileName,
            string? folder = null);
        //Download file, trả về stream (dùng cho API trả FileStreamResult).
        Task<BaseResult<Stream>> DownloadAsync(string objectKey);

        //Xoá file khỏi objectkey.
        Task<BaseResult<bool>> DeleteAsync(string objectKey);
        Task<BaseResult<object>> UpdateAsync(string objectKey, Stream stream, long size, string contentType, string fileName);
    }
}
