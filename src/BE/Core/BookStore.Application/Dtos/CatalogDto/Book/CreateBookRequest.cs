using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class CreateBookRequest
    {
        public string Title { get; set; } = null;
        public string ISBN { get; set; } = null;
        public string Description { get; set; } = null;
        public int PublicationYear { get; set; }
        public Guid PublisherId { get; set; }

    }
}
