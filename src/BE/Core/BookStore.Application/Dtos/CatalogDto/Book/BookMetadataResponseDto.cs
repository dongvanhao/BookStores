using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookMetadataResponseDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
