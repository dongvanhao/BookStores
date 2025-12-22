using BookStore.Application.Dtos.CatalogDto.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Book
{
    public static class BookMapping
    {
        public static BookDetailResponseDto ToDetailResponse(this Domain.Entities.Catalog.Book book)
        {
            return new BookDetailResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                Description = book.Description,
                PublicationYear = book.PublicationYear,
                Language = book.Language,
                IsAvailable = book.IsAvailable,
                Edition = book.Edition,
                PageCount = book.PageCount,
                CoverImageUrl = book.CoverImageUrl,

                Publisher = book.Publisher.Name,

                Authors = book.BookAuthors
                    .Select(x => x.Author.Name)
                    .ToList(),

                Categories = book.BookCategories
                    .Select(x => x.Category.Name)
                    .ToList()
            };
        }
    }
}
