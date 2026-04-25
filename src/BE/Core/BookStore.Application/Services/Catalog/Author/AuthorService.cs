using BookStore.Application.Dtos.CatalogDto.Author;
using BookStore.Application.IService.Catalog.Author;
using BookStore.Application.Mappers.Catalog.Author;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;

namespace BookStore.Application.Services.Catalog.Author
{
    public class AuthorService : IAuthorService
    {
        
        private readonly IAuthorRepository _authors;
        private readonly IDbSession _session;
        public AuthorService(IAuthorRepository authors, IDbSession session)
        {
            _authors = authors;
            _session = session;
        }

        public async Task<BaseResult<AuthorResponseDto>> CreateAsync(CreateAuthorRequestDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.Name, nameof(request.Name));
            if(error != null)
                return BaseResult<AuthorResponseDto>.Fail(error);

            var name = request.Name.NormalizeSpace();

            if (await _authors.ExistsByNameAsync(name))
                return BaseResult<AuthorResponseDto>.Fail(
                "Author.Duplicated",
                "Author already exists.",
                ErrorType.Conflict
                );
            
            var author = new Domain.Entities.Catalog.Author
            {
                Id = Guid.NewGuid(),
                Name = name,
                Biography = request.Biography,
                AvatarUrl = request.AvatarUrl
            };

            await _authors.AddAsync(author);
            await _session.SaveChangesAsync();

            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var author = await _authors.GetByIdAsync(id);

            if (author == null)
                return BaseResult<bool>.NotFound(
                    $"Author with Id '{id}' not found."
                    );

            // Nếu đã gán Book → nên chặn
            if (author.BookAuthors != null && author.BookAuthors.Any())
            {
                return BaseResult<bool>.Fail(
                    "Author.HasBooks",
                    "Author is assigned to books and cannot be deleted.",
                    ErrorType.Validation
                    );
            }
            _authors.Delete(author);
            await _session.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }

        public async Task<BaseResult<IReadOnlyList<AuthorResponseDto>>> GetAllAsync()
        {
            var authors = await _authors.GetListAsync(
                    orderBy: q => q.OrderBy(a => a.Name)
                );
            return BaseResult<IReadOnlyList<AuthorResponseDto>>.Ok(
                authors.Select(a => a.ToResponse()).ToList()
                );
        }

        public async Task<BaseResult<AuthorResponseDto>> GetByIdAsync(Guid id)
        {
            var author = await _authors.GetByIdAsync(id);

            if (author == null)
                return BaseResult<AuthorResponseDto>.NotFound(
                    $"Author with Id '{id}' not found."
                    );
            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }

        public async Task<BaseResult<AuthorResponseDto>> UpdateAsync(Guid id, UpdateAuthorRequestDto request)
        {
            var author = await _authors.GetByIdAsync(id);

            if (author == null)
                return BaseResult<AuthorResponseDto>.NotFound(
                    $"Author with Id '{id}' not found."
                    );
            author.Name = request.Name.NormalizeSpace();
            author.Biography = request.Biography;
            author.AvatarUrl = request.AvatarUrl;

            _authors.Update(author);
            await _session.SaveChangesAsync();

            return BaseResult<AuthorResponseDto>.Ok(author.ToResponse());
        }
    }
}
