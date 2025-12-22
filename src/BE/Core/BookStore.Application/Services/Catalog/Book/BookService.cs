using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Catalog.Book;
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

        public async Task<BaseResult<BookDetailResponseDto>> CreateAsync(CreateBookRequestDto request)
        {
            if (await _uow.Books.ExistsByISBNAsync(request.ISBN))
                return BaseResult<BookDetailResponseDto>.Fail(
                    "Book.DuplicatedISBN",
                    "ISBN đã tồn tại",
                    ErrorType.Conflict
                );

            var publisher = await _uow.Publishers.GetByIdAsync(request.PublisherId);
            if (publisher == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Publisher không tồn tại");

            var book = new Domain.Entities.Catalog.Book
            {
                Id = Guid.NewGuid(),
                Title = request.Title.NormalizeSpace(),
                ISBN = request.ISBN,
                Description = request.Description,
                PublicationYear = request.PublicationYear,
                Language = request.Language,
                Edition = request.Edition,
                PageCount = request.PageCount,
                PublisherId = request.PublisherId
            };

            await _uow.Books.AddAsync(book);

            // Authors
            foreach (var authorId in request.AuthorIds.Distinct())
                await _uow.BookAuthor.AddAsync(new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = authorId
                });

            // Categories
            foreach (var categoryId in request.CategoryIds.Distinct())
                await _uow.BookCategory.AddAsync(new BookCategory
                {
                    BookId = book.Id,
                    CategoryId = categoryId
                });

            await _uow.SaveChangesAsync();

            return await GetByIdAsync(book.Id);
        }
        public async Task<BaseResult<BookDetailResponseDto>> GetByIdAsync(Guid id)
        {
            var book = await _uow.Books.GetDetailAsync(id);

            if (book == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Không tìm thấy sách");

            return BaseResult<BookDetailResponseDto>.Ok(book.ToDetailResponse());
        }

        public async Task<BaseResult<PagedResult<BookDetailResponseDto>>> GetListAsync(int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BaseResult<PagedResult<BookDetailResponseDto>>.Fail(
                    "Book.InvalidPagination",
                    "Page và PageSize phải lớn hơn 0",
                    ErrorType.Validation
                );
            }

            var skip = (page - 1) * pageSize;

            var total = await _uow.Books.CountAsync();
            var books = await _uow.Books.GetPagedAsync(skip, pageSize);

            var items = books
                .Select(b => b.ToDetailResponse())
                .ToList();

            return BaseResult<PagedResult<BookDetailResponseDto>>.Ok(
                new PagedResult<BookDetailResponseDto>(
                    items,
                    total,
                    page,
                    pageSize
                )
            );
        }
            
        public async Task<BaseResult<BookDetailResponseDto>> UpdateAsync(Guid id, UpdateBookRequestDto request)
        {
           var book = await _uow.Books.GetByIdAsync(id);
              if (book == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Không tìm thấy sách");

            book.Title = request.Title.NormalizeSpace();
            book.Description = request.Description;
            book.IsAvailable = request.IsAvailable;
            book.Edition = request.Edition;
            book.PageCount = request.PageCount;

            _uow.Books.Update(book);
            await _uow.SaveChangesAsync();

            var updatedBook = await _uow.Books.GetDetailAsync(book.Id);
            return BaseResult<BookDetailResponseDto>.Ok(updatedBook!.ToDetailResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var book = await _uow.Books.GetByIdAsync(id);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");
            
            _uow.Books.Delete(book);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

    }
}
