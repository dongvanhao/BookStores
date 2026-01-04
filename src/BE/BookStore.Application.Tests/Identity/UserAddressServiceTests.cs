using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.Services.IDentity;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Shared.Common;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Tests.Identity
{
    public class UserAddressServiceTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IUserAddressRepository> _addresses = new();

        private readonly UserAddressService _service;

        public UserAddressServiceTests()
        {
            _uow.Setup(x => x.UserAddresses).Returns(_addresses.Object);

            _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new UserAddressService(_uow.Object);
        }
        [Fact]
        public async Task GetMyAsync_ShouldReturnList()
        {
            var userId = Guid.NewGuid();

            var data = new List<UserAddress>
    {
        new UserAddress { Id = Guid.NewGuid(), UserId = userId },
        new UserAddress { Id = Guid.NewGuid(), UserId = userId }
    };

            _addresses
                .Setup(x => x.GetByUserAsync(userId))
                .ReturnsAsync(data);

            var result = await _service.GetMyAsync(userId);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Count.Should().Be(2);
        }
        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnId()
        {
            var userId = Guid.NewGuid();

            var dto = new CreateUserAddressDto
            {
                ReipientName = "Nova",
                PhoneNumber = "0123456789",
                Povince = "HN",
                District = "Ba Dinh",
                Ward = "Kim Ma",
                StreetAddress = "123 Street"
            };

            var result = await _service.CreateAsync(userId, dto);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBe(Guid.Empty);

            _addresses.Verify(x => x.AddAsync(It.IsAny<UserAddress>()), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_AddressNotFound_ShouldFail()
        {
            _addresses
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((UserAddress?)null);

            var result = await _service.UpdateAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateUserAddressDto()
            );

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.NotFound);
        }
        [Fact]
        public async Task UpdateAsync_NotOwner_ShouldFail()
        {
            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid() // user khác
            };

            _addresses
                .Setup(x => x.GetByIdAsync(address.Id))
                .ReturnsAsync(address);

            var result = await _service.UpdateAsync(
                Guid.NewGuid(),
                address.Id,
                new UpdateUserAddressDto()
            );

            result.IsSuccess.Should().BeFalse();
        }
        [Fact]
        public async Task UpdateAsync_Success_ShouldUpdate()
        {
            var userId = Guid.NewGuid();

            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReipientName = "Old"
            };

            _addresses
                .Setup(x => x.GetByIdAsync(address.Id))
                .ReturnsAsync(address);

            var dto = new UpdateUserAddressDto
            {
                ReipientName = "New Name"
            };

            var result = await _service.UpdateAsync(userId, address.Id, dto);

            result.IsSuccess.Should().BeTrue();
            address.ReipientName.Should().Be("New Name");
        }
        [Fact]
        public async Task DeleteAsync_NotFound_ShouldFail()
        {
            _addresses
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((UserAddress?)null);

            var result = await _service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

            result.IsSuccess.Should().BeFalse();
        }
        [Fact]
        public async Task DeleteAsync_Success_ShouldDelete()
        {
            var userId = Guid.NewGuid();

            var address = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            _addresses
                .Setup(x => x.GetByIdAsync(address.Id))
                .ReturnsAsync(address);

            var result = await _service.DeleteAsync(userId, address.Id);

            result.IsSuccess.Should().BeTrue();

            _addresses.Verify(x => x.Delete(address), Times.Once);
        }
        [Fact]
        public async Task SetDefaultAsync_NotFound_ShouldFail()
        {
            _addresses
                .Setup(x => x.GetByUserAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<UserAddress>());

            var result = await _service.SetDefaultAsync(
                Guid.NewGuid(),
                Guid.NewGuid()
            );

            result.IsSuccess.Should().BeFalse();
        }
        [Fact]
        public async Task SetDefaultAsync_Success_ShouldSetOnlyOneDefault()
        {
            var userId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            var list = new List<UserAddress>
    {
        new UserAddress { Id = targetId, UserId = userId, IsDefault = false },
        new UserAddress { Id = Guid.NewGuid(), UserId = userId, IsDefault = true }
    };

            _addresses
                .Setup(x => x.GetByUserAsync(userId))
                .ReturnsAsync(list);

            var result = await _service.SetDefaultAsync(userId, targetId);

            result.IsSuccess.Should().BeTrue();
            list.Single(x => x.Id == targetId).IsDefault.Should().BeTrue();
            list.Single(x => x.Id != targetId).IsDefault.Should().BeFalse();
        }

    }
}
