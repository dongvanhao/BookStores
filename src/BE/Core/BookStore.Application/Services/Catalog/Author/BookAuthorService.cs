using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using BookStore.Domain.Entities.Catalog;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Author
{
    public class BookAuthorService : IBookAuthorService
    {
        private readonly IAuthorRepository _authors;
        private readonly IBookRepository _books;
        private readonly IBookAuthorRepository _bookAuthors;
        private readonly IDbSession _session;

        public BookAuthorService(IAuthorRepository authors, IBookRepository books, IBookAuthorRepository bookAuthors, IDbSession session)
        {
            _authors = authors;
            _books = books;
            _bookAuthors = bookAuthors;
            _session = session;
        }

        public async Task<BaseResult<bool>> AddAuthorsAsync(Guid bookId, List<Guid> authorIds)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<bool>.NotFound("Book not found.");

            foreach (var authorId in authorIds.Distinct())
            {
                var author = await _authors.GetByIdAsync(authorId);
                if (author == null)
                    return BaseResult<bool>.NotFound("Author not found.");

                if (await _bookAuthors.ExistsAsync(bookId, authorId))
                    continue;

                await _bookAuthors.AddAsync(new BookAuthor
                {
                    BookId = bookId,
                    AuthorId = authorId
                });
            }

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> RemoveAuthorAsync(Guid bookId, Guid authorId)
        {
            var links = await _bookAuthors.GetByBookIdAsync(bookId);
            var link = links.FirstOrDefault(x => x.AuthorId == authorId);

            if (link == null)
                return BaseResult<bool>.NotFound("Link not found.");

            await _bookAuthors.RemoveAsync(link);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<BookAuthorResponse>>> GetAuthorsAsync(Guid bookId)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<IReadOnlyList<BookAuthorResponse>>.NotFound("Book not found.");

            var items = await _bookAuthors.GetByBookIdAsync(bookId);

            return BaseResult<IReadOnlyList<BookAuthorResponse>>.Ok(
                items.Select(x => new BookAuthorResponse
                {
                    AuthorId = x.AuthorId,
                    AuthorName = x.Author.Name
                }).ToList()
            );
        }
    }
}
