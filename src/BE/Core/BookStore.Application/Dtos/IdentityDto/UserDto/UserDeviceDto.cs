using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.IdentityDto.UserDto
{
    public class UserDeviceDto
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = null!;
        public string DeviceType { get; set; } = null!;
        public string LastLoginIp { get; set; } = null!;
        public DateTime LastLoginAt { get; set; }
    }
}
