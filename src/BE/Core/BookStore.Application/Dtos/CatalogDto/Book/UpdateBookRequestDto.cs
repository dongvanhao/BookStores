using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class UpdateBookRequestDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public string? Edition { get; set; }
        public int PageCount { get; set; }

    }
}
