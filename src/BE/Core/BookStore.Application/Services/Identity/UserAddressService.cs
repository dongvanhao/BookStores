using BookStore.Application.Dtos.IdentityDto.UserDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.Mappers.Identities;
using BookStore.Domain.Entities.Identity;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;

namespace BookStore.Application.Services.Identity
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IUserAddressRepository _userAddresses;
        private readonly IDbSession _session;
        public UserAddressService(IUserAddressRepository userAddresses, IDbSession session)
        {
            _userAddresses = userAddresses;
            _session = session;
        }
        public async Task<BaseResult<IReadOnlyList<UserAddressDto>>> GetMyAsync(Guid userId)
        {
            var list = await _userAddresses.GetByUserAsync(userId);
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
                RecipientName = dto.RecipientName,
                PhoneNumber = dto.PhoneNumber,
                Province = dto.Province,
                District = dto.District,
                Ward = dto.Ward,
                StreetAddress = dto.StreetAddress
            };

            await _userAddresses.AddAsync(entity);
            await _session.SaveChangesAsync();

            return BaseResult<Guid>.Ok(entity.Id);
        }

        public async Task<BaseResult<bool>> UpdateAsync(
            Guid userId, Guid id, UpdateUserAddressDto dto)
        {
            var address = await _userAddresses.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return BaseResult<bool>.NotFound();

            address.RecipientName = dto.RecipientName;
            address.PhoneNumber = dto.PhoneNumber;
            address.Province = dto.Province;
            address.District = dto.District;
            address.Ward = dto.Ward;
            address.StreetAddress = dto.StreetAddress;

            _userAddresses.Update(address);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid userId, Guid id)
        {
            var address = await _userAddresses.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return BaseResult<bool>.NotFound();

            _userAddresses.Delete(address);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<bool>> SetDefaultAsync(Guid userId, Guid id)
        {
            var list = await _userAddresses.GetByUserAsync(userId);
            var target = list.FirstOrDefault(x => x.Id == id);
            if (target == null)
                return BaseResult<bool>.NotFound();

            foreach (var a in list)
                a.IsDefault = a.Id == id;

            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
