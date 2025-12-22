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
    public class BookImageService : IBookImageService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storage;
        public BookImageService(IUnitOfWork uow, IStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }
        public async Task<BaseResult<BookImageResponseDto>> UploadAsync(Guid bookId, IFormFile file, UploadBookImageRequestDto request)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<BookImageResponseDto>.NotFound(
                    $"Không tìm thấy sách với Id '{bookId}'.");
            }

            if(file == null || file.Length == 0)
            {
                return BaseResult<BookImageResponseDto>.Fail(
                    code: "BookImage.InvalidFile",
                    message: "Tệp hình ảnh không hợp lệ.",
                    type: ErrorType.Validation);
            }

            var upload = await _storage.UploadAsync(
                file.OpenReadStream(),
                file.Length,
                file.ContentType,
                file.FileName,
                folder: "book-images");

            if (!upload.IsSuccess)
                return BaseResult<BookImageResponseDto>.Fail(upload.Error!);

            var order = (await _uow.BookImage.GetByBookIdAsync(bookId)).Count + 1;

            var image = new Domain.Entities.Catalog.BookImage
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                ObjectName = upload.Value!,
                Url = upload.Value!,
                ContentType = file.ContentType,
                Size = file.Length,
                IsCover = request.IsCover,
                DisplayOrder = order
            };

            //Nếu set cover → hạ các ảnh khác
            if (request.IsCover)
            {
                var currentCover = await _uow.BookImage.GetCoverAsync(bookId);
                if(currentCover != null)
                    currentCover.IsCover = false;
            }
            await _uow.BookImage.AddAsync(image);
            await _uow.SaveChangesAsync();
            return BaseResult<BookImageResponseDto>.Ok(image.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookImageResponseDto>>> GetByBookIdAsync(Guid bookId)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<IReadOnlyList<BookImageResponseDto>>.NotFound(
                    $"Không tìm thấy sách với Id '{bookId}'.");
            }
            var images = await _uow.BookImage.GetByBookIdAsync(bookId);
            
            return BaseResult<IReadOnlyList<BookImageResponseDto>>.Ok(
                images.Select(i => i.ToResponse()).ToList());
        }

        public async Task<BaseResult<bool>> SetCoverAsync(Guid bookId, Guid imageId)
        {
            var image = await _uow.BookImage.GetByIdAsync(imageId);
            if (image == null || image.BookId != bookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy hình ảnh với Id '{imageId}' cho sách với Id '{bookId}'.");
            }
            var currentCover = await _uow.BookImage.GetCoverAsync(bookId);
            if (currentCover != null && currentCover.Id != imageId)
            {
                currentCover.IsCover = false;
            }
            image.IsCover = true;

            _uow.BookImage.Update(image);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid bookId, Guid imageId)
        {
            var image = await _uow.BookImage.GetByIdAsync(imageId);
            if (image == null || image.BookId != bookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy hình ảnh với Id '{imageId}' cho sách với Id '{bookId}'.");
            }
            _uow.BookImage.Delete(image);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> ReorderAsync(Guid bookId, List<Guid> imageIds)
        {
            var images = await _uow.BookImage.GetByBookIdAsync(bookId);
            if (images.Count != imageIds.Count || images.Any(i => !imageIds.Contains(i.Id)))
            {
                return BaseResult<bool>.Fail(
                    code: "BookImage.InvalidReorder",
                    message: "Danh sách hình ảnh không hợp lệ cho việc sắp xếp lại.",
                    type: ErrorType.Validation);
            }

            for (int i = 0; i < imageIds.Count; i++)
            {
                var image = images.First(img => img.Id == imageIds[i]);
                image.DisplayOrder = i + 1;
                _uow.BookImage.Update(image);
            }

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
