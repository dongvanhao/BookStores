using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.Mappers.Catalog.Book;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Book
{
    public class BookFormatService : IBookFormatService
    {
        private readonly IUnitOfWork _uow;
        public BookFormatService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BaseResult<BookFormatResponseDto>> CreateAsync(CreateBookFormatRequestDto request)
        {
            var error = Guard.AgainstNullOrWhiteSpace(request.FormatType, nameof(request.FormatType));
            if (error != null)
                return BaseResult<BookFormatResponseDto>.Fail(error);

            var type = request.FormatType.NormalizeSpace();

            if (await _uow.BookFormat.ExistsByTypeAsync(type))
                return BaseResult<BookFormatResponseDto>.Fail(
                    "BookFormat.Duplicated",
                    "Định dạng sách đã tồn tại",
                    ErrorType.Conflict
                );
            var format = new Domain.Entities.Catalog.BookFormat
            {
                Id = Guid.NewGuid(),
                FormatType = type,
                Description = request.Description
            };

            await _uow.BookFormat.AddAsync(format);
            await _uow.SaveChangesAsync();
            return BaseResult<BookFormatResponseDto>.Ok(format.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookFormatResponseDto>>> GetAllAsync()
        {
            var formats = await _uow.BookFormat.GetListAsync(
                    orderBy: q => q.OrderBy(f => f.FormatType)
            );
            return BaseResult<IReadOnlyList<BookFormatResponseDto>>.Ok(
                formats.Select(f => f.ToResponse()).ToList()
            );
        }

        public async Task<BaseResult<BookFormatResponseDto>> GetByIdAsync(Guid id)
        {
            var format = await _uow.BookFormat.GetByIdAsync(id);
            if (format == null)
                return BaseResult<BookFormatResponseDto>.NotFound(
                    $"Không tìm thấy định dạng sách với Id '{id}'."
                );
            return BaseResult<BookFormatResponseDto>.Ok(format.ToResponse());
        }

        public async Task<BaseResult<BookFormatResponseDto>> UpdateAsync(Guid id, UpdateBookFormatRequestDto request)
        {
            var format = await _uow.BookFormat.GetByIdAsync(id);
            if (format == null)
                return BaseResult<BookFormatResponseDto>.NotFound(
                    $"Không tìm thấy định dạng sách với Id '{id}'."
                );
            
            format.FormatType = request.FormatType.NormalizeSpace();
            format.Description = request.Description;

            _uow.BookFormat.Update(format);
            await _uow.SaveChangesAsync();
            return BaseResult<BookFormatResponseDto>.Ok(format.ToResponse());
        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid id)
        {
            var format = await _uow.BookFormat.GetByIdAsync(id);
            if (format == null)
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy định dạng sách với Id '{id}'."
                );
            // Nếu đã gán Book → nên chặn
            if (format.Books != null && format.Books.Any())
            {
                return BaseResult<bool>.Fail(
                    "BookFormat.HasBooks",
                    "Định dạng sách đã được gán với sách, không thể xóa.",
                    ErrorType.Validation
                );
            }
            _uow.BookFormat.Delete(format);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
