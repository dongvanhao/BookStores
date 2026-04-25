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
    public class BookImageService : IBookImageService
    {
        private readonly IBookRepository _books;
        private readonly IBookImageRepository _bookImages;
        private readonly IDbSession _session;
        private readonly IStorageService _storage;
        public BookImageService(IBookRepository books, IBookImageRepository bookImages, IDbSession session, IStorageService storage)
        {
            _books = books;
            _bookImages = bookImages;
            _session = session;
            _storage = storage;
        }
        public async Task<BaseResult<BookImageResponseDto>> UploadAsync(Guid bookId, IFormFile file, UploadBookImageRequestDto request)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<BookImageResponseDto>.NotFound(
                    $"Book with Id '{bookId}' not found.");
            }

            if(file == null || file.Length == 0)
            {
                return BaseResult<BookImageResponseDto>.Fail(
                    code: "BookImage.InvalidFile",
                    message: "Invalid image file.",
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

            var order = (await _bookImages.GetByBookIdAsync(bookId)).Count + 1;

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
                var currentCover = await _bookImages.GetCoverAsync(bookId);
                if(currentCover != null)
                    currentCover.IsCover = false;
            }
            await _bookImages.AddAsync(image);
            await _session.SaveChangesAsync();
            return BaseResult<BookImageResponseDto>.Ok(image.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookImageResponseDto>>> GetByBookIdAsync(Guid bookId)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<IReadOnlyList<BookImageResponseDto>>.NotFound(
                    $"Book with Id '{bookId}' not found.");
            }
            var images = await _bookImages.GetByBookIdAsync(bookId);
            
            return BaseResult<IReadOnlyList<BookImageResponseDto>>.Ok(
                images.Select(i => i.ToResponse()).ToList());
        }

        public async Task<BaseResult<bool>> SetCoverAsync(Guid bookId, Guid imageId)
        {
            var image = await _bookImages.GetByIdAsync(imageId);
            if (image == null || image.BookId != bookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Image with Id '{imageId}' not found for book with Id '{bookId}'.");
            }
            var currentCover = await _bookImages.GetCoverAsync(bookId);
            if (currentCover != null && currentCover.Id != imageId)
            {
                currentCover.IsCover = false;
            }
            image.IsCover = true;

            _bookImages.Update(image);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid bookId, Guid imageId)
        {
            var image = await _bookImages.GetByIdAsync(imageId);
            if (image == null || image.BookId != bookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Image with Id '{imageId}' not found for book with Id '{bookId}'.");
            }
            _bookImages.Delete(image);
            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> ReorderAsync(Guid bookId, List<Guid> imageIds)
        {
            var images = await _bookImages.GetByBookIdAsync(bookId);
            if (images.Count != imageIds.Count || images.Any(i => !imageIds.Contains(i.Id)))
            {
                return BaseResult<bool>.Fail(
                    code: "BookImage.InvalidReorder",
                    message: "Invalid image list for reordering.",
                    type: ErrorType.Validation);
            }

            for (int i = 0; i < imageIds.Count; i++)
            {
                var image = images.First(img => img.Id == imageIds[i]);
                image.DisplayOrder = i + 1;
                _bookImages.Update(image);
            }

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
