namespace BookStore.Domain.Entities.Identity
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    }
}
