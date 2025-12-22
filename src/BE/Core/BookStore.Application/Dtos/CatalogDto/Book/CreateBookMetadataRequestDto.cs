using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class CreateBookMetadataRequestDto
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
