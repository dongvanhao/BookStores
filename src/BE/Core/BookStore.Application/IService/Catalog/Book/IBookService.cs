using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Book
{
    public interface IBookService
    {
        Task<BaseResult<BookDetailResponseDto>> CreateAsync(CreateBookRequestDto request);
        Task<BaseResult<BookDetailResponseDto>> GetByIdAsync(Guid id);
        Task<BaseResult<PagedResult<BookDetailResponseDto>>> GetListAsync(int page, int pageSize);
        Task<BaseResult<BookDetailResponseDto>> UpdateAsync(Guid id, UpdateBookRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid id);
    }
}
