namespace BookStore.Domain.Entities.Identity
{
    public class UserAddress
    {
        public Guid Id { get; set; }
        public string RecipientName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        public bool IsDefault { get; set; } = false;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
