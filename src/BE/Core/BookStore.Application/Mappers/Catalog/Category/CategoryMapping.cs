using BookStore.Application.Dtos.CatalogDto.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Catalog.Category
{
    public static class CategoryMapping
    {
        public static CategoryResponseDto ToResponse(this Domain.Entities.Catalog.Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId
            };
        }
    }
}
