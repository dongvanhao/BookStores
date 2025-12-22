using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Category
{
    public class CreateCategoryRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
    }
}
