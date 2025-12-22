using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Category
{
    public class AddCategoriesToBookRequest
    {
        public List<Guid> CategoryIds { get; set; } = new();
    }
}
