using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Identities
{
    public static class UserAddressMapping
    {
        public static UserAddressDto ToDto(this UserAddress a)
        {
            return new UserAddressDto
            {
                Id = a.Id,
                RecipientName = a.RecipientName,
                PhoneNumber = a.PhoneNumber,
                Province = a.Province,
                District = a.District,
                Ward = a.Ward,
                StreetAddress = a.StreetAddress,
                IsDefault = a.IsDefault
            };
        }
    }
}
