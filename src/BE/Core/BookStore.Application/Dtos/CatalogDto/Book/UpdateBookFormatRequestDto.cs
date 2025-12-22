using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class UpdateBookFormatRequestDto
    {
        public string FormatType { get; set; } = null!; // Ví dụ: "PDF", "EPUB", "MOBI"
        public string? Description { get; set; } // Mô tả định dạng sách
    }
}
