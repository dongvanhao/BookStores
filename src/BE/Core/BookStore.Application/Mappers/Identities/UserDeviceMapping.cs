using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Mappers.Identities
{
    public static class UserDeviceMapping
    {
        public static UserDeviceDto ToDto(this UserDevice d)
        {
            return new UserDeviceDto
            {
                Id = d.Id,
                DeviceName = d.DeviceName,
                DeviceType = d.DeviceType,
                LastLoginIp = d.LastLoginIp,
                LastLoginAt = d.LastLoginAt
            };
        }
    }
}
