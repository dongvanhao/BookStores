using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class ReorderBookImageRequestDto
    {
        public List<Guid> ImageIds { get; set; } = new();
    }
}
