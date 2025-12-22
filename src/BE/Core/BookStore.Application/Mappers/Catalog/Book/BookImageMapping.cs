using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Book
{
    public static class BookImageMapping
    {
        public static BookImageResponseDto ToResponse(this BookImage image)
        {
            return new BookImageResponseDto
            {
                Id = image.Id,
                Url = image.Url,
                IsCover = image.IsCover,
                DisplayOrder = image.DisplayOrder
            };
        }
    }
}
