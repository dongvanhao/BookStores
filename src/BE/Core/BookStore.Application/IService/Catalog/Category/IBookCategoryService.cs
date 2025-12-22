using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Category
{
    public interface IBookCategoryService
    {
        Task<BaseResult<bool>> AddCategoriesAsync(Guid bookId, List<Guid> categoryIds);
        Task<BaseResult<bool>> RemoveCategoryAsync(Guid bookId, Guid categoryId);
        Task<BaseResult<IReadOnlyList<BookCategoryResponse>>> GetCategoriesAsync(Guid bookId);
    }
}
