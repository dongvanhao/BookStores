using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookImageResponseDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = null!;
        public bool IsCover { get; set; }
        public int DisplayOrder { get; set; }
    }
}
