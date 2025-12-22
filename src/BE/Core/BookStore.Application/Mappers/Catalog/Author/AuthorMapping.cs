using BookStore.Application.Dtos.CatalogDto.Author;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Author
{
    public static class AuthorMapping
    {
        public static AuthorResponseDto ToResponse(this Domain.Entities.Catalog.Author author)
        {
            return new AuthorResponseDto
            {
                Id = author.Id,
                Name = author.Name,
                Biography = author.Biography,
                AvartarUrl = author.AvartarUrl
            };
        }
    }
}
