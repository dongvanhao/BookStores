using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Entities.Identity
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        // store hash of token for security
        public string TokenHash { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByIp { get; set; } = "";
        public string? ReplacedByTokenHash { get; set; } // rotation tracking
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
