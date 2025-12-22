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
    public interface IBookImageService
    {
        Task<BaseResult<BookImageResponseDto>> UploadAsync(Guid bookId, IFormFile file,UploadBookImageRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid bookId, Guid imageId);
        Task<BaseResult<bool>> SetCoverAsync(Guid bookId, Guid imageId);
        Task<BaseResult<IReadOnlyList<BookImageResponseDto>>> GetByBookIdAsync(Guid bookId);
        Task<BaseResult<bool>> ReorderAsync(Guid bookId, List<Guid> imageIds);
    }
}
