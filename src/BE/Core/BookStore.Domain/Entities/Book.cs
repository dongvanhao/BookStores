using BookStore.Domain.Common;

namespace BookStore.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ISBN { get; private set; } = string.Empty;
    public int PublishedYear { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string? CoverUrl { get; private set; }
    public bool IsDeleted { get; private set; }

    // FK
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    // Navigation
    public ICollection<BookAuthor> BookAuthors { get; private set; } = [];
    public ICollection<Review> Reviews { get; private set; } = [];
    public ICollection<OrderItem> OrderItems { get; private set; } = [];

    private Book() { }

    public static Book Create(
        string title,
        string? description,
        string isbn,
        int publishedYear,
        decimal price,
        int stockQuantity,
        Guid categoryId)
    {
        return new Book
        {
            Id            = Guid.NewGuid(),
            Title         = title,
            Description   = description,
            ISBN          = isbn,
            PublishedYear = publishedYear,
            Price         = price,
            StockQuantity = stockQuantity,
            CategoryId    = categoryId,
            IsDeleted     = false,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string? description,
        string isbn,
        int publishedYear,
        decimal price,
        int stockQuantity,
        Guid categoryId)
    {
        Title         = title;
        Description   = description;
        ISBN          = isbn;
        PublishedYear = publishedYear;
        Price         = price;
        StockQuantity = stockQuantity;
        CategoryId    = categoryId;
        UpdatedAt     = DateTime.UtcNow;
    }

    public void SetCover(string coverUrl)
    {
        CoverUrl  = coverUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    // Giảm tồn kho khi đặt hàng — trả về false nếu không đủ hàng
    public bool TryReduceStock(int quantity)
    {
        if (StockQuantity < quantity) return false;

        StockQuantity -= quantity;
        UpdatedAt      = DateTime.UtcNow;
        return true;
    }

    public void RestoreStock(int quantity)
    {
        StockQuantity += quantity;
        UpdatedAt      = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
