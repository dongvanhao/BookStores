using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities.Catalog
{
    public class BookImage
    {
        public Guid Id { get; set; }

        public string ObjectName { get; set; } = null!;  // Tên file trong MinIO
        public string Url { get; set; } = null!;         // URL public hoặc presigned
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }
        public bool IsCover { get; set; }                     // Ảnh này có phải bìa chính không
        public int DisplayOrder { get; set; }                 // Thứ tự hiển thị ảnh

        public Guid BookId { get; set; }
        public virtual Book Book { get; set; } = null!;
    }
}
