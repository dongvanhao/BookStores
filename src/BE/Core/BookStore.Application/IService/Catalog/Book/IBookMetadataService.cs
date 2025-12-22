using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Book
{
    public interface IBookMetadataService
    {
        Task<BaseResult<BookMetadataResponseDto>> CreateAsync(Guid bookId,CreateBookMetadataRequestDto request);
        Task<BaseResult<IReadOnlyList<BookMetadataResponseDto>>> GetByBookAsync(Guid bookId);
        Task<BaseResult<BookMetadataResponseDto>> UpdateAsync(Guid bookId, Guid metadataId, UpdateBookMetadataRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid bookId, Guid metadataId);
    }
}
