using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Catalog.Book;
using BookStore.Domain.Entities.Catalog;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog
{

    public class BookService : IBookService
    {
        private readonly IBookRepository _books;
        private readonly IPublisherRepository _publishers;
        private readonly IBookAuthorRepository _bookAuthors;
        private readonly IBookCategoryRepository _bookCategories;
        private readonly IDbSession _session;
        private readonly IStorageService _storage;
        public BookService(IBookRepository books, IPublisherRepository publishers, IBookAuthorRepository bookAuthors, IBookCategoryRepository bookCategories, IDbSession session, IStorageService storage)
        {
            _books = books;
            _publishers = publishers;
            _bookAuthors = bookAuthors;
            _bookCategories = bookCategories;
            _session = session;
            _storage = storage;
        }

        public async Task<BaseResult<BookDetailResponseDto>> CreateAsync(CreateBookRequestDto request)
        {
            if (await _books.ExistsByISBNAsync(request.ISBN))
                return BaseResult<BookDetailResponseDto>.Fail(
                    "Book.DuplicatedISBN",
                    "ISBN already exists.",
                    ErrorType.Conflict
                );

            var publisher = await _publishers.GetByIdAsync(request.PublisherId);
            if (publisher == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Publisher not found.");

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

            await _books.AddAsync(book);

            foreach (var authorId in request.AuthorIds.Distinct())
                await _bookAuthors.AddAsync(new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = authorId
                });

            foreach (var categoryId in request.CategoryIds.Distinct())
                await _bookCategories.AddAsync(new BookCategory
                {
                    BookId = book.Id,
                    CategoryId = categoryId
                });

            await _session.SaveChangesAsync();

            return await GetByIdAsync(book.Id);
        }

        public async Task<BaseResult<BookDetailResponseDto>> GetByIdAsync(Guid id)
        {
            var book = await _books.GetDetailAsync(id);

            if (book == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Book not found.");

            return BaseResult<BookDetailResponseDto>.Ok(book.ToDetailResponse());
        }

        public async Task<BaseResult<PagedResult<BookDetailResponseDto>>> GetListAsync(int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
                return BaseResult<PagedResult<BookDetailResponseDto>>.Fail(
                    "Book.InvalidPagination",
                    "Page and PageSize must be greater than 0.",
                    ErrorType.Validation
                );

            if (pageSize > 50)
                pageSize = 50;

            var skip = (page - 1) * pageSize;

            var total = await _books.CountAsync();
            var books = await _books.GetPagedAsync(skip, pageSize);

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

        public async Task<BaseResult<PagedResult<BookDetailResponseDto>>> SearchAsync(BookSearchQuery query)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = Math.Clamp(query.PageSize, 1, 50);
            var skip = (page - 1) * pageSize;

            var (books, total) = await _books.SearchAsync(query.Keyword, query.AuthorId, query.CategoryId, skip, pageSize);

            var items = books.Select(b => b.ToDetailResponse()).ToList();

            return BaseResult<PagedResult<BookDetailResponseDto>>.Ok(
                new PagedResult<BookDetailResponseDto>(items, total, page, pageSize));
        }

        public async Task<BaseResult<BookDetailResponseDto>> UpdateAsync(Guid id, UpdateBookRequestDto request)
        {
            var book = await _books.GetByIdAsync(id);
            if (book == null)
                return BaseResult<BookDetailResponseDto>.NotFound("Book not found.");

            book.Title = request.Title.NormalizeSpace();
            book.Description = request.Description;
            book.IsAvailable = request.IsAvailable;
            book.Edition = request.Edition;
            book.PageCount = request.PageCount;

            _books.Update(book);
            await _session.SaveChangesAsync();

            var updatedBook = await _books.GetDetailAsync(book.Id);
            return BaseResult<BookDetailResponseDto>.Ok(updatedBook!.ToDetailResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var book = await _books.GetByIdAsync(id);
            if (book == null)
                return BaseResult<bool>.NotFound("Book not found.");

            _books.Delete(book);
            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
