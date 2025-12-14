using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
    }
}
