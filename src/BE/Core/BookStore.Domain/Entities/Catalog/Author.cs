namespace BookStore.Domain.Entities.Catalog
{
    public class Author : BaseAuditableEntity
    {
        public string Name { get; private set; } = null!;
        public string? Biography { get; private set; }
        public string? AvatarUrl { get; private set; }

        public virtual ICollection<BookAuthor> BookAuthors { get; set; } = [];
    }
}
