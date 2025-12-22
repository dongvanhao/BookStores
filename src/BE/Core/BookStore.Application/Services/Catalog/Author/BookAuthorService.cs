using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Author
{
    public class BookAuthorService : IBookAuthorService
    {
        private readonly IUnitOfWork _uow;

        public BookAuthorService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<bool>> AddAuthorsAsync(Guid bookId, List<Guid> authorIds)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");

            foreach (var authorId in authorIds.Distinct())
            {
                var author = await _uow.Author.GetByIdAsync(authorId);
                if (author == null)
                    return BaseResult<bool>.NotFound("Tác giả không tồn tại");

                if (await _uow.BookAuthor.ExistsAsync(bookId, authorId))
                    continue;

                await _uow.BookAuthor.AddAsync(new BookAuthor
                {
                    BookId = bookId,
                    AuthorId = authorId
                });
            }

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> RemoveAuthorAsync(Guid bookId, Guid authorId)
        {
            var links = await _uow.BookAuthor.GetByBookIdAsync(bookId);
            var link = links.FirstOrDefault(x => x.AuthorId == authorId);

            if (link == null)
                return BaseResult<bool>.NotFound("Liên kết không tồn tại");

            await _uow.BookAuthor.RemoveAsync(link);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<BookAuthorResponse>>> GetAuthorsAsync(Guid bookId)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<IReadOnlyList<BookAuthorResponse>>.NotFound("Không tìm thấy sách");

            var items = await _uow.BookAuthor.GetByBookIdAsync(bookId);

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
