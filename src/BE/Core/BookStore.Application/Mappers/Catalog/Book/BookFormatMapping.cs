using BookStore.Application.Dtos.CatalogDto.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Book
{
    public static class BookFormatMapping
    {
        public static BookFormatResponseDto ToResponse(this Domain.Entities.Catalog.BookFormat fomat)
        {
            return new BookFormatResponseDto
            {
                Id = fomat.Id,
                FormatType = fomat.FormatType,
                Description = fomat.Description
            };
        }
    }
}
