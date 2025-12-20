using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog
{

    public class BookService : IBookService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storage;
        public BookService(IUnitOfWork uow, IStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }

        public async Task<BaseResult<Guid>> CreateAsync(
    CreateBookRequest request,
    IFormFile? coverImage)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Title, nameof(request.Title))
                     ?? Guard.AgainstNullOrWhiteSpace(request.ISBN, nameof(request.ISBN));

            if (error != null)
                return BaseResult<Guid>.Fail(error);

            var publisher = await _uow.Publishers.GetByIdAsync(request.PublisherId);
            if (publisher == null)
                return BaseResult<Guid>.NotFound("Publisher không tồn tại");

            var book = new Book
            {
                Id = Guid.NewGuid(),
                Title = request.Title.NormalizeSpace(),
                ISBN = request.ISBN.NormalizeSpace(),
                Description = request.Description?.NormalizeSpace() ?? string.Empty,
                PublicationYear = request.PublicationYear,
                PublisherId = request.PublisherId
            };


            await _uow.ExecuteTransactionAsync(async () =>
            {
                await _uow.Books.AddAsync(book);

                if (coverImage == null) return;

                var upload = await _storage.UploadAsync(
                    coverImage.OpenReadStream(),
                    coverImage.Length,
                    coverImage.ContentType,
                    coverImage.FileName,
                    $"books/covers/{book.Id}");

                if (!upload.IsSuccess)
                    throw new InvalidOperationException(upload.Error!.Message);

                book.Images.Add(new BookImage
                {
                    Id = Guid.NewGuid(),
                    BookId = book.Id,
                    ObjectName = upload.Value!,
                    ContentType = coverImage.ContentType,
                    Size = coverImage.Length,
                    IsCover = true,
                    Url = $"/api/file/{upload.Value}"

                });
            });

            return BaseResult<Guid>.Ok(book.Id);
        }



        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var book = await _uow.Books.GetByIdAsync(id);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");

            _uow.Books.DeleteAsync(book);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }


        public async Task<BaseResult<BookResponse>> GetByIdAsync(Guid id)
        {
            var book = await _uow.Books.GetDetailAsync(id);
            if (book == null)
                return BaseResult<BookResponse>.NotFound("Không tìm thấy sách");

            return BaseResult<BookResponse>.Ok(new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                CoverImageUrl = book.CoverImageUrl
            });
        }


        public async Task<BaseResult<bool>> UpdateAsync(Guid id, UpdateBookRequest request)
        {
            var book = await _uow.Books.GetByIdAsync(id);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");

            book.Title = request.Title.NormalizeSpace();
            book.Description = request.Description.NormalizeSpace();
            book.PublicationYear = request.PublicationYear;

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }


        public async Task<BaseResult<bool>> UpdateCoverAsync(Guid id, IFormFile coverImage)
        {
            var book = await _uow.Books.GetDetailAsync(id);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");

            try
            {
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    foreach (var img in book.Images)
                        img.IsCover = false;

                    var upload = await _storage.UploadAsync(
                        coverImage.OpenReadStream(),
                        coverImage.Length,
                        coverImage.ContentType,
                        coverImage.FileName,
                        $"books/covers/{book.Id}");

                    if (!upload.IsSuccess)
                        throw new Exception(upload.Error!.Message);

                    book.Images.Add(new BookImage
                    {
                        Id = Guid.NewGuid(),
                        BookId = book.Id,
                        ObjectName = upload.Value!,
                        ContentType = coverImage.ContentType,
                        Size = coverImage.Length,
                        IsCover = true
                    });

                    book.CoverImageUrl = $"/api/file/{upload.Value}";
                });

                return BaseResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return BaseResult<bool>.Fail(CommonErrors.InternalServerError(ex.Message));
            }
        }

    }
}
