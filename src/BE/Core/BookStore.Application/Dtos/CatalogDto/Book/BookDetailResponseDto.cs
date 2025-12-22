using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class BookDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string ISBN { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int PublicationYear { get; set; }
        public string Language { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public string? Edition { get; set; }
        public int PageCount { get; set; }
        public string? CoverImageUrl { get; set; }

        public string Publisher { get; set; } = null!;
        public List<string> Authors { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }
}
