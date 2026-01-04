using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.Mappers.Identities;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUnitOfWork _uow;
        public UserAddressService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<BaseResult<IReadOnlyList<UserAddressDto>>> GetMyAsync(Guid userId)
        {
            var list = await _uow.UserAddresses.GetByUserAsync(userId);
            return BaseResult<IReadOnlyList<UserAddressDto>>.Ok(
                list.Select(x => x.ToDto()).ToList()
            );
        }

        public async Task<BaseResult<Guid>> CreateAsync(Guid userId, CreateUserAddressDto dto)
        {
            var entity = new UserAddress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReipientName = dto.ReipientName,
                PhoneNumber = dto.PhoneNumber,
                Povince = dto.Povince,
                District = dto.District,
                Ward = dto.Ward,
                StreetAddress = dto.StreetAddress
            };

            await _uow.UserAddresses.AddAsync(entity);
            await _uow.SaveChangesAsync();

            return BaseResult<Guid>.Ok(entity.Id);
        }

        public async Task<BaseResult<bool>> UpdateAsync(
            Guid userId, Guid id, UpdateUserAddressDto dto)
        {
            var address = await _uow.UserAddresses.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return BaseResult<bool>.NotFound();

            address.ReipientName = dto.ReipientName;
            address.PhoneNumber = dto.PhoneNumber;
            address.Povince = dto.Povince;
            address.District = dto.District;
            address.Ward = dto.Ward;
            address.StreetAddress = dto.StreetAddress;

            _uow.UserAddresses.Update(address);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid userId, Guid id)
        {
            var address = await _uow.UserAddresses.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return BaseResult<bool>.NotFound();

            _uow.UserAddresses.Delete(address);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> SetDefaultAsync(Guid userId, Guid id)
        {
            var list = await _uow.UserAddresses.GetByUserAsync(userId);
            var target = list.FirstOrDefault(x => x.Id == id);
            if (target == null)
                return BaseResult<bool>.NotFound();

            foreach (var a in list)
                a.IsDefault = a.Id == id;

            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
