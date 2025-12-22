using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Category
{
    public class UpdateCategoryRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
