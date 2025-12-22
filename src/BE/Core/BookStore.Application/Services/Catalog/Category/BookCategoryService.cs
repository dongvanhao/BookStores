using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Category
{
    public class BookCategoryService : IBookCategoryService
    {
        private readonly IUnitOfWork _uow;

        public BookCategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<bool>> AddCategoriesAsync(
            Guid bookId, List<Guid> categoryIds)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<bool>.NotFound("Không tìm thấy sách");

            foreach (var categoryId in categoryIds.Distinct())
            {
                var category = await _uow.Category.GetByIdAsync(categoryId);
                if (category == null)
                    return BaseResult<bool>.NotFound("Danh mục không tồn tại");

                if (await _uow.BookCategory.ExistsAsync(bookId, categoryId))
                    continue;

                await _uow.BookCategory.AddAsync(new BookCategory
                {
                    BookId = bookId,
                    CategoryId = categoryId
                });
            }

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> RemoveCategoryAsync(
            Guid bookId, Guid categoryId)
        {
            var links = await _uow.BookCategory.GetByBookIdAsync(bookId);
            var link = links.FirstOrDefault(x => x.CategoryId == categoryId);

            if (link == null)
                return BaseResult<bool>.NotFound("Liên kết không tồn tại");

            await _uow.BookCategory.RemoveAsync(link);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<BookCategoryResponse>>> GetCategoriesAsync(Guid bookId)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<IReadOnlyList<BookCategoryResponse>>.NotFound("Không tìm thấy sách");

            var items = await _uow.BookCategory.GetByBookIdAsync(bookId);

            return BaseResult<IReadOnlyList<BookCategoryResponse>>.Ok(
                items.Select(x => new BookCategoryResponse
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category.Name
                }).ToList()
            );
        }
    }
}
