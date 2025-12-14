using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities.Catalog
{
    public class BookFile
    {
        public Guid Id { get; set; }

        public string ObjectName { get; set; } = null!;   // Tên file lưu trong MinIO
        public string Url { get; set; } = null!;          // Link download
        public string FileType { get; set; } = null!;         // Loại file
        public long FileSize { get; set; }                    // Dung lượng file (bytes)
        public bool IsPreview { get; set; }                   // Có phải bản xem thử không

        public Guid BookId { get; set; }
        public virtual Book Book { get; set; } = null!;
    }
}
