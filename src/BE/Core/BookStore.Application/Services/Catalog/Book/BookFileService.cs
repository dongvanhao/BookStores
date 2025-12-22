using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Catalog.Book;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Book
{
    public class BookFileService : IBookFileService
    {
       
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storage;
        public BookFileService(IUnitOfWork uow, IStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }
        public async Task<BaseResult<BookFileResponseDto>> UploadAsync(Guid bookId, IFormFile file, UploadBookFileRequestDto request)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<BookFileResponseDto>.NotFound(
                    $"Không tìm thấy sách với Id '{bookId}'."
                );
            }
            if (file == null || file.Length == 0)
            {
                return BaseResult<BookFileResponseDto>.Fail(
                    code: "BookFile.InvalidFile",
                    message: "Tệp tin không hợp lệ hoặc trống.",
                    type: ErrorType.Validation
                );
            }

            //upload file to storage (e.g., MinIO, AWS S3, etc.) and get the URL
            var uploadResult = await _storage.UploadAsync(
            file.OpenReadStream(),
            file.Length,
            file.ContentType,
            file.FileName,
            folder: "book-files"
            );

            if (!uploadResult.IsSuccess)
            {
                return BaseResult<BookFileResponseDto>.Fail(
                    code: "BookFile.UploadFailed",
                    message: "Tải tệp tin lên kho lưu trữ thất bại.",
                    type: ErrorType.Internal
                );
            }

            var bookFile = new Domain.Entities.Catalog.BookFile
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                ObjectName = uploadResult.Value!,
                Url = uploadResult.Value!, // nếu MinIO trả URL
                FileType = request.FileType,
                FileSize = file.Length,
                IsPreview = request.IsPreview
            };

            await _uow.BookFile.AddAsync(bookFile);
            await _uow.SaveChangesAsync();

            return BaseResult<BookFileResponseDto>.Ok(bookFile.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookFileResponseDto>>> GetAllByBookIdAsync(Guid BookId)
        {
            var book = await _uow.Books.GetByIdAsync(BookId);
            if (book == null)
            {
                return BaseResult<IReadOnlyList<BookFileResponseDto>>.NotFound(
                    $"Không tìm thấy sách với Id '{BookId}'."
                );
            }
            var files = await _uow.BookFile.GetByBookIdAsync(BookId);

            return BaseResult<IReadOnlyList<BookFileResponseDto>>.Ok(
                files.Select(f => f.ToResponse()).ToList()
            );
        }
        public async Task<BaseResult<bool>> DeleteAsync(Guid BookId, Guid fileId)
        {
            var file = await _uow.BookFile.GetByIdAsync(fileId);

            if (file == null || file.BookId != BookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy tệp tin với Id '{fileId}' cho sách với Id '{BookId}'."
                );
            }
            _uow.BookFile.Delete(file);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
