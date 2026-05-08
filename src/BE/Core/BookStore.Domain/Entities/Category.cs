using BookStore.Domain.Common;
using BookStore.Domain.Errors;
using BookStore.Shared.Results;

namespace BookStore.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Self-referencing — null nếu là root category
    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = [];

    // MinIO — null nếu chưa có icon
    public string? IconObjectKey { get; private set; }
    public Guid?   IconMediaId   { get; private set; }

    // Navigation
    public ICollection<Book> Books { get; private set; } = [];

    private Category() { }

    public static Category Create(string name, string? description, Guid? parentId = null)
    {
        return new Category
        {
            Id          = Guid.NewGuid(),
            Name        = name,
            Description = description,
            ParentId    = parentId,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };
    }

    public Result Update(string name, string? description, Guid? parentId)
    {
        if (parentId == Id)
            return Result.Failure(CategoryErrors.SelfParent);

        Name        = name;
        Description = description;
        ParentId    = parentId;
        UpdatedAt   = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateIcon(string objectKey, Guid mediaId)
    {
        IconObjectKey = objectKey;
        IconMediaId   = mediaId;
        UpdatedAt     = DateTime.UtcNow;
    }

    public void RemoveIcon()
    {
        IconObjectKey = null;
        IconMediaId   = null;
        UpdatedAt     = DateTime.UtcNow;
    }
}
