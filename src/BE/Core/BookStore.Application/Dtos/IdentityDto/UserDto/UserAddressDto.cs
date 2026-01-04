using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.IdentityDto.UserDto
{
    public class UserAddressDto
    {
        public Guid Id { get; set; }
        public string ReipientName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Povince { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}
