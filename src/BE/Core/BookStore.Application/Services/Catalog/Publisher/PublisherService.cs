using BookStore.Application.Dtos.CatalogDto.Publisher;
using BookStore.Application.IService.Catalog.Publisher;
using BookStore.Application.Mappers.Catalog.Publisher;
using BookStore.Domain.Entities.Catalog;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Publisher
{
    public class PublisherService : IPublisherService
    {
        private readonly IPublisherRepository _publishers;
        private readonly IDbSession _session;
        public PublisherService(IPublisherRepository publishers, IDbSession session)
        {
            _publishers = publishers;
            _session = session;
        }
        public async Task<BaseResult<PublisherResponseDto>> CreateAsync(CreatePublisherDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if (error != null)
            {
                return BaseResult<PublisherResponseDto>.Fail(error);
            }
            var name = request.Name.NormalizeSpace();

            if (await _publishers.ExitsByNameAsync(name))
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.Exists",
                    message: $"Publisher with name '{name}' already exists.",
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
            await _publishers.AddAsync(publisher);
            await _session.SaveChangesAsync();
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var publisher = await _publishers.GetByIdAsync(id);

            if(publisher == null)
            {
                return BaseResult<bool>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Publisher with Id '{id}' not found.",
                    type: ErrorType.NotFound);
            }

            _publishers.Delete(publisher);
            await _session.SaveChangesAsync();

            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<PublisherResponseDto>>> GetAllAsync()
        {
            var publishers = await _publishers.GetListAsync(
                    orderBy: q => q.OrderBy(p => p.Name)
                );
            return BaseResult<IReadOnlyList<PublisherResponseDto>>.Ok(
                publishers.Select(p => p.ToResponse()).ToList()
                );
        }

        public async Task<BaseResult<PublisherResponseDto>> GetByIdAsync(Guid id)
        {
            var publisher = await _publishers.GetByIdAsync(id);
            if (publisher == null)
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Publisher with Id '{id}' not found.",
                    type: ErrorType.NotFound);
            }
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }

        public async Task<BaseResult<PublisherResponseDto>> UpdateAsync(Guid id, UpdatePublisherRequestDto request)
        {
            var publisher = await _publishers.GetByIdAsync(id);
            if (publisher == null)
            {
                return BaseResult<PublisherResponseDto>.Fail(
                    code: "Publisher.NotFound",
                    message: $"Publisher with Id '{id}' not found.",
                    type: ErrorType.NotFound);
            }
            publisher.Name = request.Name.NormalizeSpace();
            publisher.Address = request.Address;
            publisher.Email = request.Email;
            publisher.PhoneNumber = request.PhoneNumber;

            _publishers.Update(publisher);
            await _session.SaveChangesAsync();
            return BaseResult<PublisherResponseDto>.Ok(publisher.ToResponse());
        }
    }
}
