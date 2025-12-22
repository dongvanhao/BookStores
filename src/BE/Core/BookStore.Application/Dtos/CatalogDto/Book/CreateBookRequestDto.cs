using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class CreateBookRequestDto
    {
        public string Title { get; set; } = null!;
        public string ISBN { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int PublicationYear { get; set; }
        public string Language { get; set; } = "vi";
        public string? Edition { get; set; }
        public int PageCount { get; set; }

        public Guid PublisherId { get; set; }

        // gán ngay khi tạo
        public List<Guid> AuthorIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();

    }
}
