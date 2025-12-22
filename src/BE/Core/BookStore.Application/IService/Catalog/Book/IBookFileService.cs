using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Book
{
    public interface IBookFileService
    {
        Task<BaseResult<BookFileResponseDto>> UploadAsync(
        Guid bookId,
        IFormFile file,
        UploadBookFileRequestDto request);

        Task<BaseResult<IReadOnlyList<BookFileResponseDto>>> GetAllByBookIdAsync(Guid bookId);

        Task<BaseResult<bool>> DeleteAsync(Guid id, Guid fileId);
    }
}
