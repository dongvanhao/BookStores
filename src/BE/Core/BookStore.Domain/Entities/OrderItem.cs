using BookStore.Domain.Common;

namespace BookStore.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Snapshot tên sách tại thời điểm đặt hàng — tránh bị ảnh hưởng nếu sách bị đổi tên
    public string BookTitle { get; private set; } = string.Empty;

    public decimal SubTotal => UnitPrice * Quantity;

    // FK
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    public Guid BookId { get; private set; }
    public Book Book { get; private set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid bookId, string bookTitle, int quantity, decimal unitPrice)
    {
        return new OrderItem
        {
            Id        = Guid.NewGuid(),
            OrderId   = orderId,
            BookId    = bookId,
            BookTitle = bookTitle,
            Quantity  = quantity,
            UnitPrice = unitPrice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
