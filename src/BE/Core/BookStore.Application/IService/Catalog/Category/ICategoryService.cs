using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Category
{
    public interface ICategoryService
    {
        Task<BaseResult<CategoryResponseDto>> CreateAsync(CreateCategoryRequestDto request);
        Task<BaseResult<IReadOnlyList<CategoryResponseDto>>> GetAllAsync();
        Task<BaseResult<IReadOnlyList<CategoryTreeResponseDto>>> GetTreeAsync();
        Task<BaseResult<CategoryResponseDto>> GetByIdAsync(Guid id);
        Task<BaseResult<CategoryResponseDto>> UpdateAsync(Guid id, UpdateCategoryRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid id);
    }
}
