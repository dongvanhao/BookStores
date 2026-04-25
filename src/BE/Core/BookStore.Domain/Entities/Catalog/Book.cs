using BookStore.Domain.Common;
using BookStore.Domain.Entities.Pricing_Inventory;

namespace BookStore.Domain.Entities.Catalog
{
    public class Book : BaseAuditableEntity
    {

        public string Title { get; private set; } = null!;
        public string ISBN { get; private set; } = null!;
        public string? Description { get; private set; }
        public int PublicationYear { get; private set; }
        public string? Language { get; private set; }
        public string? Edition { get; private set; }
        public int? PageCount { get;  private set; }
        public decimal Price { get; private set; }
        public bool IsAvailable { get; private set; } = true;

        public Guid PublisherId { get; set; }
        public virtual Publisher Publisher { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = [];
        public virtual ICollection<BookCategory> BookCategories { get; set; } = [];
        public virtual ICollection<BookImage> Images { get; set; } = [];
        public virtual BookFile? PreviewFile { get; set; }
        public virtual StockItem? StockItem { get; set; }

        private Book() {}

        public static Book Create(
            string title,
            string isbn,
            int publicationYear,
            decimal price,
            Guid publisherId
        )
        {
            SetTitle(title);
        SetISBN(isbn);
        SetPublicationYear(publicationYear);
        SetPrice(price);

        PublisherId = publisherId;
        }
    }
}
