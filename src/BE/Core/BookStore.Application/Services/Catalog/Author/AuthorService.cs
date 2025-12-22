using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using BookStore.Application.Mappers.Catalog.Author;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Author
{
    public class AuthorService : IAuthorService
    {
        
        private readonly IUnitOfWork _uow;
        public AuthorService( IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<AuthorResponseDto>> CreateAsync(CreateAuthorRequestDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if(error != null)
                return BaseResult<AuthorResponseDto>.Fail(error);

            var name = request.Name.NormalizeSpace();

            if (await _uow.Author.ExistsByNameAsync(name))
                return BaseResult<AuthorResponseDto>.Fail(
                "Author.Duplicated",
                "Tác giả đã tồn tại",
                ErrorType.Conflict
                );
            
            var author = new Domain.Entities.Catalog.Author
            {
                Id = Guid.NewGuid(),
                Name = name,
                Biography = request.Biography,
                AvartarUrl = request.AvatarUrl
            };

            await _uow.Author.AddAsync(author);
            await _uow.SaveChangesAsync();

            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var author = await _uow.Author.GetByIdAsync(id);

            if (author == null)
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy tác giả với Id '{id}'."
                    );

            // Nếu đã gán Book → nên chặn
            if (author.BookAuthor != null && author.BookAuthor.Any())
            {
                return BaseResult<bool>.Fail(
                    "Author.HasBooks",
                    "Tác giả đã được gán với sách, không thể xóa.",
                    ErrorType.Validation
                    );
            }
            _uow.Author.Delete(author);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<AuthorResponseDto>>> GetAllAsync()
        {
            var authors = await _uow.Author.GetListAsync(
                    orderBy: q => q.OrderBy(a => a.Name)
                );
            return BaseResult<IReadOnlyList<AuthorResponseDto>>.Ok(
                authors.Select(a => a.ToResponse()).ToList()
                );
        }

        public async Task<BaseResult<AuthorResponseDto>> GetByIdAsync(Guid id)
        {
            var author = await _uow.Author.GetByIdAsync(id);

            if (author == null)
                return BaseResult<AuthorResponseDto>.NotFound(
                    $"Không tìm thấy tác giả với Id '{id}'."
                    );
            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }

        public async Task<BaseResult<AuthorResponseDto>> UpdateAsync(Guid id, UpdateAuthorRequestDto request)
        {
            var author = await _uow.Author.GetByIdAsync(id);

            if (author == null)
                return BaseResult<AuthorResponseDto>.NotFound(
                    $"Không tìm thấy tác giả với Id '{id}'."
                    );
            author.Name = request.Name.NormalizeSpace();
            author.Biography = request.Biography;
            author.AvartarUrl = request.AvatarUrl;

            _uow.Author.Update(author);
            await _uow.SaveChangesAsync();

            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }
    }
}
