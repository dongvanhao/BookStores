using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookFormatResponseDto
    {
        public Guid Id { get; set; }
        public string FormatType { get; set; } = null!; // ePub, PDF, MOBI, Hardcover, Paperback, Audiobook
        public string? Description { get; set; } 
    }
}
