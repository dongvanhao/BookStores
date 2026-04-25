namespace BookStore.Domain.Common;

public class BaseEntity
{
    public Guid Id { get; private set; } 

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}