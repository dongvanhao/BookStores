using BookStore.Application.Services.IDentity;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Shared.Common;
using FluentAssertions;
using Moq;
using Xunit;
using System.Threading;

namespace BookStore.Application.Tests.Identity
{
    public class UserDeviceServiceTests
    {
        private UserDeviceService CreateService(
            Mock<IUserDeviceRepository> repoMock,
            Mock<IUnitOfWork> uowMock)
        {
            return new UserDeviceService(
                repoMock.Object,
                uowMock.Object
            );
        }

        [Fact]
        public async Task Remove_Should_Return_NotFound_When_Device_Not_Exists()
        {
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();

            var repoMock = new Mock<IUserDeviceRepository>();
            var uowMock = new Mock<IUnitOfWork>();

            repoMock
                .Setup(r => r.GetByIdAsync(deviceId))
                .ReturnsAsync((UserDevice?)null);

            var service = CreateService(repoMock, uowMock);

            var result = await service.RemoveAsync(userId, deviceId);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.NotFound);
        }

        [Fact]
        public async Task Remove_Should_Return_NotFound_When_Device_Not_Owned()
        {
            var userId = Guid.NewGuid();
            var device = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            var repoMock = new Mock<IUserDeviceRepository>();
            var uowMock = new Mock<IUnitOfWork>();

            repoMock
                .Setup(r => r.GetByIdAsync(device.Id))
                .ReturnsAsync(device);

            var service = CreateService(repoMock, uowMock);

            var result = await service.RemoveAsync(userId, device.Id);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.NotFound);
        }

        [Fact]
        public async Task Remove_Should_Delete_Device_When_Valid()
        {
            var userId = Guid.NewGuid();
            var device = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            var repoMock = new Mock<IUserDeviceRepository>();
            var uowMock = new Mock<IUnitOfWork>();

            repoMock
                .Setup(r => r.GetByIdAsync(device.Id))
                .ReturnsAsync(device);

            // 🔥 FIX QUAN TRỌNG NHẤT
            uowMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var service = CreateService(repoMock, uowMock);

            var result = await service.RemoveAsync(userId, device.Id);

            repoMock.Verify(r => r.Delete(device), Times.Once);
            uowMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }
    }
}
