using BookStore.Application.Dtos.CatalogDto.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Publisher
{
    public static class PublisherMapping
    {
        public static PublisherResponseDto ToResponse(this Domain.Entities.Catalog.Publisher publisher)
        {
            return new PublisherResponseDto
            {
                Id = publisher.Id,
                Name = publisher.Name,
                Address = publisher.Address,
                Email = publisher.Email,
                PhoneNumber = publisher.PhoneNumber
            };
        }
    }
}
