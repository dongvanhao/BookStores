using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Author
{
    public class UpdateAuthorRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Biography { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
