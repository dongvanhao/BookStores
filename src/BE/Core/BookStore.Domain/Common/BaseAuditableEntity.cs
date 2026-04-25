namespace BookStore.Domain.Common;

public class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    protected BaseAuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkDeleted()
    {
        DeletedAt = DateTime.UtcNow;
    }
}