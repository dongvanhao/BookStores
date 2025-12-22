using BookStore.Application.Dtos.CatalogDto.Publisher;
using BookStore.Application.IService.Catalog.Publisher;
using BookStore.Application.Mappers.Catalog.Publisher;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Publisher
{
    public class PublisherService : IPublisherService
    {
        private readonly IUnitOfWork _uow;
        public PublisherService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<BaseResult<PublisherResponseDto>> CreateAsync(CreatePublisherDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if (error != null)
            {
                return BaseResult<PublisherResponseDto>.Fail(error);
            }
            var name = request.Name.NormalizeSpace();

            if (await _uow.Publishers.ExitsByNameAsync(name))
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.Exists",
                    message: $"Nhà xuất bản với tên '{name}' đã tồn tại.",
                    type: ErrorType.Validation);
            }
            var publisher = new Domain.Entities.Catalog.Publisher
            {
                Id = Guid.NewGuid(),
                Name = name,
                Address = request.Address,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber
            };
            await _uow.Publishers.AddAsync(publisher);
            await _uow.SaveChangesAsync();
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var publisher = await _uow.Publishers.GetByIdAsync(id);

            if(publisher == null)
            {
                return BaseResult<bool>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Không tìm thấy nhà xuất bản với Id '{id}'.",
                    type: ErrorType.NotFound);
            }

            _uow.Publishers.Delete(publisher);
            await _uow.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<PublisherResponseDto>>> GetAllAsync()
        {
            var publishers = await _uow.Publishers.GetListAsync(
                    orderBy: q => q.OrderBy(p => p.Name)
                );
            return BaseResult<IReadOnlyList<PublisherResponseDto>>.Ok(
                publishers.Select(p => p.ToResponse()).ToList()
                );
        }

        public async Task<BaseResult<PublisherResponseDto>> GetByIdAsync(Guid id)
        {
            var publisher = await _uow.Publishers.GetByIdAsync(id);
            if (publisher == null)
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Không tìm thấy nhà xuất bản với Id '{id}'.",
                    type: ErrorType.NotFound);
            }
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }

        public async Task<BaseResult<PublisherResponseDto>> UpdateAsync(Guid id, UpdatePublisherRequestDto request)
        {
            var publisher = await _uow.Publishers.GetByIdAsync(id);
            if (publisher == null)
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Không tìm thấy nhà xuất bản với Id '{id}'.",
                    type: ErrorType.NotFound);
            }
            publisher.Name = request.Name.NormalizeSpace();
            publisher.Address = request.Address;
            publisher.Email = request.Email;
            publisher.PhoneNumber = request.PhoneNumber;

            _uow.Publishers.Update(publisher);
            await _uow.SaveChangesAsync();
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }
    }
}
