using BookStore.Application.IService.Storage;
using BookStore.Application.Options;
using BookStore.Shared.Common;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.MinIO
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minio;
        private readonly MinIOOptions _options;

        public MinioStorageService(
            IMinioClient minio,
            IOptions<MinIOOptions> options)
        {
            _minio = minio;
            _options = options.Value;
        }

        public async Task<BaseResult<string>> UploadAsync(
            Stream stream,
            long size,
            string contextType,
            string fileName,
            string? folder = null)
        {
            try
            {
                if (stream == null || size <= 0)
                    return BaseResult<string>.Fail("Luồng không hợp lệ", "Luồng được cung cấp là null hoặc trống.", ErrorType.Validation);

                var ext = Path.GetExtension(fileName);
                var safeName = $"{Guid.NewGuid()}{ext}";

                var objectName = string.IsNullOrEmpty(folder) ? safeName : $"{folder.TrimEnd('/')}/{safeName}";

                stream.Position = 0;

                var args = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(size)
                    .WithContentType(contextType);

                await _minio.PutObjectAsync(args);

                return BaseResult<string>.Ok(objectName);
            }
            catch (Exception ex)
            {
                return BaseResult<string>.Fail(CommonErrors.InternalServerError(ex.Message));
            }
        }

        public async Task<BaseResult<Stream>> DownloadAsync(string objectKey)
        {
            try
            {
                var ms = new MemoryStream();

                var args = new GetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(ms);
                    });
                await _minio.GetObjectAsync(args);

                ms.Position = 0;
                return BaseResult<Stream>.Ok(ms);
            }
            catch (Exception ex)
            {
                return BaseResult<Stream>.NotFound("file không tồn tại hoặc không thể tải xuống: " + ex.Message);
            }
        }

        public async Task<BaseResult<bool>> DeleteAsync(string objectKey)
        {
            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey);
                await _minio.RemoveObjectAsync(args);
                return BaseResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return BaseResult<bool>.Fail(CommonErrors.InternalServerError(ex.Message));
            }
        }

        public async Task<BaseResult<object>> UpdateAsync(string objectKey, Stream stream, long size, string contentType, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                    return BaseResult<object>.Fail("Khóa đối tượng không hợp lệ", "Khóa đối tượng được cung cấp là null hoặc trống.", ErrorType.Validation);

                if (stream == null || size <= 0)
                    return BaseResult<object>.Fail("Luồng không hợp lệ", "Luồng được cung cấp là null hoặc trống.", ErrorType.Validation);

                //Always overwrite → MinIO không có update, chỉ có put
                stream.Position = 0;

                var args = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey) // Ghi đè lên objectKey cu
                    .WithStreamData(stream)
                    .WithObjectSize(size)
                    .WithContentType(contentType);

                await _minio.PutObjectAsync(args);

                return BaseResult<object>.Ok(new
                {
                    ObjectKey = objectKey,
                    ContentType = contentType,
                    FileName = fileName,
                    Size = size
                });
            }
            catch (Exception ex)
            {
                return BaseResult<object>.Fail(CommonErrors.InternalServerError(ex.Message));
            }
        }
    }
}
