using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Identities;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storages;
        public UserProfileService(IUnitOfWork uow, IStorageService storages)
        {
            _uow = uow;
            _storages = storages;
        }
        public async Task<BaseResult<UserProfileDto>> GetMyProfileAsync(Guid userId)
        {
            var profile = await _uow.UserProfiles.GetByUserIdAsync(userId);

            if (profile == null)
                return BaseResult<UserProfileDto>.NotFound("Chưa có hồ sơ người dùng");

            return BaseResult<UserProfileDto>.Ok(profile.ToDto());
        }

        public async Task<BaseResult<bool>> UpdateMyProfileAsync(
            Guid userId, UpdateUserProfileDto dto)
        {
            var profile = await _uow.UserProfiles.GetByUserIdAsync(userId);

            if (profile == null)
            {
                // LẦN ĐẦU TẠO PROFILE
                profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                };

                await _uow.UserProfiles.AddAsync(profile);
            }

            profile.FullName = dto.FullName;
            profile.DateOfBirth = dto.DateOfBirth;
            profile.Gender = dto.Gender;
            profile.AvatarUrl = dto.AvatarUrl;
            profile.PhoneNumber = dto.PhoneNumber;
            profile.Bio = dto.Bio;

            _uow.UserProfiles.Update(profile);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }
        public async Task<BaseResult<string>> UploadAvatarAsync(
    Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BaseResult<string>.Fail(
                    "Avatar.InvalidFile",
                    "File không hợp lệ",
                    ErrorType.Validation
                );

            // Lấy profile
            var profile = await _uow.UserProfiles.GetByUserIdAsync(userId);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                };
                await _uow.UserProfiles.AddAsync(profile);
            }

            // Upload lên MinIO
            using var stream = file.OpenReadStream();

            var uploadResult = await _storages.UploadAsync(
                stream,
                file.Length,
                file.ContentType,
                file.FileName,
                folder: $"avatars/{userId}"
            );

            if (!uploadResult.IsSuccess)
                return BaseResult<string>.Fail(uploadResult.Error!);

            profile.AvatarUrl = uploadResult.Value!;
            _uow.UserProfiles.Update(profile);
            await _uow.SaveChangesAsync();

            return BaseResult<string>.Ok(profile.AvatarUrl);
        }
    }
}
