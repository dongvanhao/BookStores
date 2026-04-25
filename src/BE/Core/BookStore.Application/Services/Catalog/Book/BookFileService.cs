using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Catalog.Book;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Book
{
    public class BookFileService : IBookFileService
    {
       
        private readonly IBookRepository _books;
        private readonly IBookFileRepository _bookFiles;
        private readonly IDbSession _session;
        private readonly IStorageService _storage;
        public BookFileService(IBookRepository books, IBookFileRepository bookFiles, IDbSession session, IStorageService storage)
        {
            _books = books;
            _bookFiles = bookFiles;
            _session = session;
            _storage = storage;
        }
        public async Task<BaseResult<BookFileResponseDto>> UploadAsync(Guid bookId, IFormFile file, UploadBookFileRequestDto request)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<BookFileResponseDto>.NotFound(
                    $"Book with Id '{bookId}' not found."
                );
            }
            if (file == null || file.Length == 0)
            {
                return BaseResult<BookFileResponseDto>.Fail(
                    code: "BookFile.InvalidFile",
                    message: "Invalid or empty file.",
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
                    message: "Failed to upload file to storage.",
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

            await _bookFiles.AddAsync(bookFile);
            await _session.SaveChangesAsync();

            return BaseResult<BookFileResponseDto>.Ok(bookFile.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookFileResponseDto>>> GetAllByBookIdAsync(Guid BookId)
        {
            var book = await _books.GetByIdAsync(BookId);
            if (book == null)
            {
                return BaseResult<IReadOnlyList<BookFileResponseDto>>.NotFound(
                    $"Không tìm thấy sách với Id '{BookId}'."
                );
            }
            var files = await _bookFiles.GetByBookIdAsync(BookId);

            return BaseResult<IReadOnlyList<BookFileResponseDto>>.Ok(
                files.Select(f => f.ToResponse()).ToList()
            );
        }
        public async Task<BaseResult<bool>> DeleteAsync(Guid BookId, Guid fileId)
        {
            var file = await _bookFiles.GetByIdAsync(fileId);

            if (file == null || file.BookId != BookId)
            {
                return BaseResult<bool>.NotFound(
                    $"File with Id '{fileId}' not found for book with Id '{BookId}'."
                );
            }
            _bookFiles.Delete(file);
            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
