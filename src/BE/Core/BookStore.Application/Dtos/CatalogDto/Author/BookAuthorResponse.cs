using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Author
{
    public class BookAuthorResponse
    {
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = null!;
    }
}
