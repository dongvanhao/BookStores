using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Book
{
    public interface IBookFormatService
    {
        Task<BaseResult<BookFormatResponseDto>> CreateAsync(CreateBookFormatRequestDto request);
        Task<BaseResult<IReadOnlyList<BookFormatResponseDto>>> GetAllAsync();
        Task<BaseResult<BookFormatResponseDto>> GetByIdAsync(Guid id);
        Task<BaseResult<BookFormatResponseDto>> UpdateAsync(Guid id, UpdateBookFormatRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid id);
    }
}
