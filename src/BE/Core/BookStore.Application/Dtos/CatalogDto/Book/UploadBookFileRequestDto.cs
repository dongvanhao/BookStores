using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.CatalogDto.Book
{
    public class UploadBookFileRequestDto
    {
        public string FileType { get; set; } = null!; // e.g., "pdf", "epub", "mobi"
        public bool IsPreview { get; set; } = false; // Indicates if the file is a preview version
    }
}
