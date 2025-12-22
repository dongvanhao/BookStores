using BookStore.Application.Dtos.CatalogDto.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Book
{
    public static class BookMetadataMapping
    {
        public static BookMetadataResponseDto ToResponse(this Domain.Entities.Catalog.BookMetadata entity)
        {
            return new BookMetadataResponseDto
            {
                Id = entity.Id,
                Key = entity.Key,
                Value = entity.Value
            };
        }
    }
}
