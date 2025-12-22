using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Category
{
    public class BookCategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
    }
}
