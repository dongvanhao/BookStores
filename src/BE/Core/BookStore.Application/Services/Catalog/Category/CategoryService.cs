using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Application.Mappers.Catalog.Category;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;
        public CategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<BaseResult<CategoryResponseDto>> CreateAsync(CreateCategoryRequestDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if (error is not null)
                return BaseResult<CategoryResponseDto>.Fail(error);

            if (request.ParentId.HasValue)
            {
                var parentCategory = await _uow.Category.GetByIdAsync(request.ParentId.Value);
                if (parentCategory is null)
                    return BaseResult<CategoryResponseDto>.Fail("Category.ParentNotFound",
                                                                 "Danh mục cha không tồn tại", ErrorType.NotFound);
            }

            var category = new Domain.Entities.Catalog.Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId
            };

            await _uow.Category.AddAsync(category);
            await _uow.SaveChangesAsync();
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<CategoryResponseDto>>> GetAllAsync()
        {
            var items = await _uow.Category.GetListAsync(
                orderBy: q => q.OrderBy(c => c.Name)
            );

            return BaseResult<IReadOnlyList<CategoryResponseDto>>.Ok(items.Select(c => c.ToResponse()).ToList());
        }

        public async Task<BaseResult<IReadOnlyList<CategoryTreeResponseDto>>> GetTreeAsync()
        {
            var roots = await _uow.Category.GetRootAsync();

            List<CategoryTreeResponseDto> BuildTree(IEnumerable<Domain.Entities.Catalog.Category> nodes)
            {
                return nodes.Select(c => new CategoryTreeResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Children = BuildTree(
                        _uow.Category.GetChildrenAsync(c.Id).Result
                    )
                }).ToList();
            }

            return BaseResult<IReadOnlyList<CategoryTreeResponseDto>>.Ok(
                BuildTree(roots)
            );
        }

        public async Task<BaseResult<CategoryResponseDto>> GetByIdAsync(Guid id)
        {
            var category = await _uow.Category.GetByIdAsync(id);
            if (category is null)
                return BaseResult<CategoryResponseDto>.NotFound(
                    $"Không tìm thấy danh mục với Id '{id}'."
                    );
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<CategoryResponseDto>> UpdateAsync(Guid id, UpdateCategoryRequestDto request)
        {
            var category = await _uow.Category.GetByIdAsync(id);
            if (category is null)
                return BaseResult<CategoryResponseDto>.NotFound(
                    $"Không tìm thấy danh mục với Id '{id}'."
                    );

            category.Name = request.Name.NormalizeSpace();
            category.Description = request.Description;

            _uow.Category.Update(category);
            await _uow.SaveChangesAsync();
            return BaseResult<CategoryResponseDto>.Ok(category.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var category = await _uow.Category.GetByIdAsync(id);
            if (category is null)
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy danh mục với Id '{id}'."
                    );

            if (category.SubCategories.Any())
                return BaseResult<bool>.Fail(
                    "Category.HasSubCategories",
                    "Không thể xóa danh mục vì nó có danh mục con.",
                    ErrorType.Conflict
                    );

            if (category.BookCategories.Any())
                return BaseResult<bool>.Fail(
                    "Category.HasBooks",
                    "Không thể xóa danh mục vì nó đã được gán với sách.",
                    ErrorType.Conflict
                    );

            _uow.Category.Delete(category);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
