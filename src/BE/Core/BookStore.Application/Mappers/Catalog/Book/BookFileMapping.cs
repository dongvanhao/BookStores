using BookStore.Application.Dtos.CatalogDto.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Book
{
    public static class BookFileMapping
    {
        public static Dtos.CatalogDto.Book.BookFileResponseDto ToResponse(this Domain.Entities.Catalog.BookFile file)
        {
            
            return new BookFileResponseDto
            {
                Id = file.Id,
                Url = file.Url,
                FileType = file.FileType,
                FileSize = file.FileSize,
                IsPreview = file.IsPreview
            };
        }
    }
}
