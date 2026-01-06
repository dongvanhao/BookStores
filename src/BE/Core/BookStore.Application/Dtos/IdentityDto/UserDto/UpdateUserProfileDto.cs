using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.IdentityDto.UserDto
{
    public class UpdateUserProfileDto
    {
        public string FullName { get; set; } 
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }

    }
}
