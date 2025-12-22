using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Author
{
    public interface IBookAuthorService
    {
        Task<BaseResult<bool>> AddAuthorsAsync(Guid bookId, List<Guid> authorIds);
        Task<BaseResult<bool>> RemoveAuthorAsync(Guid bookId, Guid authorId);
        Task<BaseResult<IReadOnlyList<BookAuthorResponse>>> GetAuthorsAsync(Guid bookId);
    }
}
