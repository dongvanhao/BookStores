using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Application.Mappers.Catalog.Category;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categories;
        private readonly IDbSession _session;
        public CategoryService(ICategoryRepository categories, IDbSession session)
        {
            _categories = categories;
            _session = session;
        }
        public async Task<BaseResult<CategoryResponseDto>> CreateAsync(CreateCategoryRequestDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if (error is not null)
                return BaseResult<CategoryResponseDto>.Fail(error);

            if (request.ParentId.HasValue)
            {
                var parentCategory = await _categories.GetByIdAsync(request.ParentId.Value);
                if (parentCategory is null)
                    return BaseResult<CategoryResponseDto>.Fail("Category.ParentNotFound",
                                                                 "Parent category not found.", ErrorType.NotFound);
            }

            var category = new Domain.Entities.Catalog.Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId
            };

            await _categories.AddAsync(category);
            await _session.SaveChangesAsync();
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<CategoryResponseDto>>> GetAllAsync()
        {
            var items = await _categories.GetListAsync(
                orderBy: q => q.OrderBy(c => c.Name)
            );

            return BaseResult<IReadOnlyList<CategoryResponseDto>>.Ok(items.Select(c => c.ToResponse()).ToList());
        }

        public async Task<BaseResult<IReadOnlyList<CategoryTreeResponseDto>>> GetTreeAsync()
        {
            var roots = await _categories.GetRootAsync();

            List<CategoryTreeResponseDto> BuildTree(IEnumerable<Domain.Entities.Catalog.Category> nodes)
            {
                return nodes.Select(c => new CategoryTreeResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Children = BuildTree(
                        _categories.GetChildrenAsync(c.Id).Result
                    )
                }).ToList();
            }

            return BaseResult<IReadOnlyList<CategoryTreeResponseDto>>.Ok(
                BuildTree(roots)
            );
        }

        public async Task<BaseResult<CategoryResponseDto>> GetByIdAsync(Guid id)
        {
            var category = await _categories.GetByIdAsync(id);
            if (category is null)
                return BaseResult<CategoryResponseDto>.NotFound(
                    $"Category with Id '{id}' not found."
                    );
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<CategoryResponseDto>> UpdateAsync(Guid id, UpdateCategoryRequestDto request)
        {
            var category = await _categories.GetByIdAsync(id);
            if (category is null)
                return BaseResult<CategoryResponseDto>.NotFound(
                    $"Category with Id '{id}' not found."
                    );

            category.Name = request.Name.NormalizeSpace();
            category.Description = request.Description;

            _categories.Update(category);
            await _session.SaveChangesAsync();
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var category = await _categories.GetByIdAsync(id);
            if (category is null)
                return BaseResult<bool>.NotFound(
                    $"Category with Id '{id}' not found."
                    );

            if (category.SubCategories.Any())
                return BaseResult<bool>.Fail(
                    "Category.HasSubCategories",
                    "Cannot delete a category that has subcategories.",
                    ErrorType.Conflict
                    );

            if (category.BookCategories.Any())
                return BaseResult<bool>.Fail(
                    "Category.HasBooks",
                    "Cannot delete a category that is assigned to books.",
                    ErrorType.Conflict
                    );

            _categories.Delete(category);
            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
