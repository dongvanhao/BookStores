using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class CreateBookFormatRequestDto
    {
        public string FormatType { get; set; } = null!; // e.g., "PDF", "ePub", "Mobi"
        public string? Description { get; set; }
    }
}
