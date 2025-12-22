using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Author
{
    public class AddAuthorsToBookRequest
    {
        public List<Guid> AuthorIds { get; set; } = new();
    }
}
