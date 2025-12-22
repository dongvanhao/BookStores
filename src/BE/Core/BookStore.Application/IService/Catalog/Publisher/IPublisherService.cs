using BookStore.Application.Dtos.CatalogDto.Publisher;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Publisher
{
    public interface IPublisherService
    {
        Task<BaseResult<PublisherResponseDto>> CreateAsync(CreatePublisherDto request);
        Task<BaseResult<IReadOnlyList<PublisherResponseDto>>> GetAllAsync();
        Task<BaseResult<PublisherResponseDto>> GetByIdAsync(Guid id);
        Task<BaseResult<PublisherResponseDto>> UpdateAsync(Guid id, UpdatePublisherRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid id);
    }
}
