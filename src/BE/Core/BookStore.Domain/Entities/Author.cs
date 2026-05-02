using BookStore.Domain.Common;

namespace BookStore.Domain.Entities;

public class Author : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }

    // Navigation — many-to-many với Book
    public ICollection<Book> Books { get; private set; } = [];

    private Author() { }

    public static Author Create(string fullName, string? bio)
    {
        return new Author
        {
            Id        = Guid.NewGuid(),
            FullName  = fullName,
            Bio       = bio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string fullName, string? bio)
    {
        FullName  = fullName;
        Bio       = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
