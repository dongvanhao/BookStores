using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Identity
{
    public interface IUserAddressService
    {
        Task<BaseResult<IReadOnlyList<UserAddressDto>>> GetMyAsync(Guid userId);
        Task<BaseResult<Guid>> CreateAsync(Guid userId, CreateUserAddressDto dto);
        Task<BaseResult<bool>> UpdateAsync(Guid userId, Guid id, UpdateUserAddressDto dto);
        Task<BaseResult<bool>> DeleteAsync(Guid userId, Guid id);
        Task<BaseResult<bool>> SetDefaultAsync(Guid userId, Guid id);
    }
}
