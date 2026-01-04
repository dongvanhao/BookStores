using BookStore.Application.Dtos.IdentityDto.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BookStore.Application.Mappers.Identities
{
    public static class UserProfileMapping
    {
        public static UserProfileDto ToDto(this Domain.Entities.Identity.UserProfile profile)
        {
            return new UserProfileDto
            {
                FullName = profile.FullName,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender,
                AvatarUrl = profile.AvatarUrl,
                PhoneNumber = profile.PhoneNumber,
                Bio = profile.Bio
            };
        }
    }
}
