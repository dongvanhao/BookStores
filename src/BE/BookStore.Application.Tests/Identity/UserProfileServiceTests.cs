using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Storage;
using BookStore.Application.Services.IDentity;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Shared.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Owin;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Tests.Identity
{
    public class UserProfileServiceTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IUserProfileRepository> _profiles = new();
        private readonly Mock<IStorageService> _storage = new();

        private readonly UserProfileService _service;

        public UserProfileServiceTests()
        {
            _uow.Setup(x => x.UserProfiles).Returns(_profiles.Object);

            _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new UserProfileService(
                _uow.Object,
                _storage.Object
            );
        }
        [Fact]
        public async Task GetMyProfile_ProfileNotExist_ShouldReturnNotFound()
        {
            _profiles
                .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((UserProfile?)null);

            var result = await _service.GetMyProfileAsync(Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.NotFound);
        }
        [Fact]
        public async Task GetMyProfile_ProfileExist_ShouldReturnDto()
        {
            var profile = new UserProfile
            {
                UserId = Guid.NewGuid(),
                FullName = "Nova"
            };

            _profiles
                .Setup(x => x.GetByUserIdAsync(profile.UserId))
                .ReturnsAsync(profile);

            var result = await _service.GetMyProfileAsync(profile.UserId);

            result.IsSuccess.Should().BeTrue();
            result.Value!.FullName.Should().Be("Nova");
        }
        [Fact]
        public async Task UpdateMyProfile_FirstTime_ShouldCreateProfile()
        {
            var userId = Guid.NewGuid();

            _profiles
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync((UserProfile?)null);

            var dto = new UpdateUserProfileDto
            {
                FullName = "Nova",
                PhoneNumber = "0123456789"
            };

            var result = await _service.UpdateMyProfileAsync(userId, dto);

            result.IsSuccess.Should().BeTrue();

            _profiles.Verify(x => x.AddAsync(It.IsAny<UserProfile>()), Times.Once);
        }
        [Fact]
        public async Task UpdateMyProfile_ExistingProfile_ShouldUpdate()
        {
            var userId = Guid.NewGuid();

            var profile = new UserProfile
            {
                UserId = userId,
                FullName = "Old Name"
            };

            _profiles
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);

            var dto = new UpdateUserProfileDto
            {
                FullName = "New Name"
            };

            var result = await _service.UpdateMyProfileAsync(userId, dto);

            result.IsSuccess.Should().BeTrue();
            profile.FullName.Should().Be("New Name");
        }
        private static IFormFile CreateFakeFile()
        {
            var content = "fake image content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "avatar", "avatar.png")
            {
                Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(),
                ContentType = "image/png"
            };
        }
        [Fact]
        public async Task UploadAvatar_FileNull_ShouldFail()
        {
            var result = await _service.UploadAvatarAsync(Guid.NewGuid(), null!);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Validation);
        }
        [Fact]
        public async Task UploadAvatar_StorageFail_ShouldReturnFail()
        {
            var userId = Guid.NewGuid();

            _profiles
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new UserProfile { UserId = userId });

            _storage
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<long>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ))
                .ReturnsAsync(BaseResult<string>.Fail(
                    "Upload.Fail",
                    "Upload error",
                    ErrorType.Internal
                ));

            var file = CreateFakeFile();

            var result = await _service.UploadAvatarAsync(userId, file);

            result.IsSuccess.Should().BeFalse();
        }
        [Fact]
        public async Task UploadAvatar_Success_ShouldUpdateAvatarUrl()
        {
            var userId = Guid.NewGuid();
            var profile = new UserProfile { UserId = userId };

            _profiles
                .Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(profile);

            _storage
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<long>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ))
                .ReturnsAsync(BaseResult<string>.Ok("avatars/path.png"));

            var file = CreateFakeFile();

            var result = await _service.UploadAvatarAsync(userId, file);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be("avatars/path.png");
            profile.AvatarUrl.Should().Be("avatars/path.png");
        }

    }
}
