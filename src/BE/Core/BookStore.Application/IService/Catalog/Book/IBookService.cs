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
    public interface IBookService
    {
        Task<BaseResult<Guid>> CreateAsync(CreateBookRequest request, IFormFile? coverImage);
        Task<BaseResult<BookResponse>> GetByIdAsync(Guid id);
        Task<BaseResult<bool>> UpdateAsync(Guid id, UpdateBookRequest request);
        Task<BaseResult<bool>> UpdateCoverAsync(Guid id, IFormFile coverImage);
        Task<BaseResult<bool>> DeleteAsync(Guid id);


    }
}
