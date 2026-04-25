using BookStore.Application.Dtos.CatalogDto.Category;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Domain.Entities.Catalog;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Category
{
    public class BookCategoryService : IBookCategoryService
    {
        private readonly IBookRepository _books;
        private readonly IBookCategoryRepository _bookCategories;
        private readonly ICategoryRepository _categories;
        private readonly IDbSession _session;

        public BookCategoryService(IBookRepository books, IBookCategoryRepository bookCategories, ICategoryRepository categories, IDbSession session)
        {
            _books = books;
            _bookCategories = bookCategories;
            _categories = categories;
            _session = session;
        }

        public async Task<BaseResult<bool>> AddCategoriesAsync(
            Guid bookId, List<Guid> categoryIds)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<bool>.NotFound("Book not found.");

            foreach (var categoryId in categoryIds.Distinct())
            {
                var category = await _categories.GetByIdAsync(categoryId);
                if (category == null)
                    return BaseResult<bool>.NotFound("Category not found.");

                if (await _bookCategories.ExistsAsync(bookId, categoryId))
                    continue;

                await _bookCategories.AddAsync(new BookCategory
                {
                    BookId = bookId,
                    CategoryId = categoryId
                });
            }

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> RemoveCategoryAsync(
            Guid bookId, Guid categoryId)
        {
            var links = await _bookCategories.GetByBookIdAsync(bookId);
            var link = links.FirstOrDefault(x => x.CategoryId == categoryId);

            if (link == null)
                return BaseResult<bool>.NotFound("Link not found.");

            await _bookCategories.RemoveAsync(link);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<BookCategoryResponse>>> GetCategoriesAsync(Guid bookId)
        {
            var book = await _books.GetByIdAsync(bookId);
            if (book == null)
                return BaseResult<IReadOnlyList<BookCategoryResponse>>.NotFound("Book not found.");

            var items = await _bookCategories.GetByBookIdAsync(bookId);

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
