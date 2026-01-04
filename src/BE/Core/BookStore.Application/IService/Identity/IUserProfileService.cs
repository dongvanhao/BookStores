using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Identity
{
    public interface IUserProfileService
    {
        Task<BaseResult<UserProfileDto>> GetMyProfileAsync(Guid userId);
        Task<BaseResult<bool>> UpdateMyProfileAsync(Guid userId, UpdateUserProfileDto Dto);
        Task<BaseResult<string>> UploadAvatarAsync(Guid userId, IFormFile file);
    }
}
