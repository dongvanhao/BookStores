using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookFileResponseDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public long FileSize { get; set; }
        public bool IsPreview { get; set; }
    }
}
