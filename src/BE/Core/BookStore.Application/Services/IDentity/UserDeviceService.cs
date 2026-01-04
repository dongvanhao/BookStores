using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.Mappers.Identities;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class UserDeviceService : IUserDeviceService
    {
        private readonly IUnitOfWork _uow;
        private readonly IUserDeviceRepository _userDeviceRepo;

        public UserDeviceService(IUserDeviceRepository userDeviceRepo, IUnitOfWork uow)
        {
            _userDeviceRepo = userDeviceRepo;
            _uow = uow;
        }

        public async Task<BaseResult<IReadOnlyList<UserDeviceDto>>> GetMyDevicesAsync(Guid userId)
        {
            var devices = await _uow.UserDevices.GetByUserAsync(userId);

            return BaseResult<IReadOnlyList<UserDeviceDto>>.Ok(
                devices.Select(d => d.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<bool>> RemoveAsync(Guid userId, Guid deviceId)
        {
            var device = await _userDeviceRepo.GetByIdAsync(deviceId);
            if (device == null || device.UserId != userId)
                return BaseResult<bool>.NotFound();

            _userDeviceRepo.Delete(device);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }
    }
}
