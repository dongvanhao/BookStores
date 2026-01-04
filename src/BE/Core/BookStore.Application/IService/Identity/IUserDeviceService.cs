using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Identity
{
    public interface IUserDeviceService
    {
        Task<BaseResult<IReadOnlyList<UserDeviceDto>>> GetMyDevicesAsync(Guid userId);
        Task<BaseResult<bool>> RemoveAsync(Guid userId, Guid deviceId);
    }
}
