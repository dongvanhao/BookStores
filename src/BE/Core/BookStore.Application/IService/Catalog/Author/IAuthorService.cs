using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Catalog.Author
{
    public interface IAuthorService
    {
        Task<BaseResult<AuthorResponseDto>> CreateAsync(CreateAuthorRequestDto request);
        Task<BaseResult<IReadOnlyList<AuthorResponseDto>>> GetAllAsync();
        Task<BaseResult<AuthorResponseDto>> GetByIdAsync(Guid id);
        Task<BaseResult<AuthorResponseDto>> UpdateAsync(Guid id, UpdateAuthorRequestDto request);
        Task<BaseResult<bool>> DeleteAsync(Guid id);
    }
}
