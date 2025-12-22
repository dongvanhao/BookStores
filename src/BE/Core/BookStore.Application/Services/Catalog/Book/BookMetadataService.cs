using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Storage;
using BookStore.Application.Mappers.Catalog.Book;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Catalog.Book
{
    public class BookMetadataService : IBookMetadataService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStorageService _storage;
        public BookMetadataService(IUnitOfWork uow, IStorageService storage)
        {
            _uow = uow;
            _storage = storage;
        }

        public async Task<BaseResult<BookMetadataResponseDto>> CreateAsync(Guid bookId, CreateBookMetadataRequestDto request)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<BookMetadataResponseDto>.NotFound(
                    $"Không tìm thấy sách với Id '{bookId}'.");
            }
           var keyError = Guard.AgainstNullOrWhiteSpace(request.Key, nameof(request.Key));
            if (keyError != null)
                return BaseResult<BookMetadataResponseDto>.Fail(keyError);

            var valueError = Guard.AgainstNullOrWhiteSpace(request.Value, nameof(request.Value));
            if (valueError != null)
                return BaseResult<BookMetadataResponseDto>.Fail(valueError);
            var key = request.Key.NormalizeSpace();

            if (await _uow.BookMetadata.ExistsKeyAsync(bookId, key))
                return BaseResult<BookMetadataResponseDto>.Fail(
                    "Metadata.DuplicatedKey",
                    "Thuộc tính đã tồn tại",
                    ErrorType.Conflict
                );

            var metadata = new Domain.Entities.Catalog.BookMetadata
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                Key = key,
                Value = request.Value
            };

            await _uow.BookMetadata.AddAsync(metadata);
            await _uow.SaveChangesAsync();
            return BaseResult<BookMetadataResponseDto>.Ok(metadata.ToResponse());
        }

        public async Task<BaseResult<IReadOnlyList<BookMetadataResponseDto>>> GetByBookAsync(Guid bookId)
        {
            var book = await _uow.Books.GetByIdAsync(bookId);
            if (book == null)
            {
                return BaseResult<IReadOnlyList<BookMetadataResponseDto>>.NotFound(
                    $"Không tìm thấy sách với Id '{bookId}'.");
            }
            var metadataList = await _uow.BookMetadata.GetByBookIdAsync(bookId);

            return BaseResult<IReadOnlyList<BookMetadataResponseDto>>.Ok(
                metadataList.Select(x => x.ToResponse()).ToList()
            );
        }

        public async Task<BaseResult<BookMetadataResponseDto>> UpdateAsync(
            Guid bookId, Guid metadataId,UpdateBookMetadataRequestDto request)
        {
            var meta = await _uow.BookMetadata.GetByIdAsync(metadataId);

            if (meta == null || meta.BookId != bookId)
            {
                return BaseResult<BookMetadataResponseDto>.NotFound(
                    $"Không tìm thấy metadata với Id '{metadataId}' cho sách với Id '{bookId}'.");
            }

            var valueError = Guard.AgainstNullOrWhiteSpace(request.Value, nameof(request.Value));
            if (valueError != null)
                return BaseResult<BookMetadataResponseDto>.Fail(valueError);

            meta.Value = request.Value;

            _uow.BookMetadata.Update(meta);
            await _uow.SaveChangesAsync();
            return BaseResult<BookMetadataResponseDto>.Ok(meta.ToResponse());

        }

        public async Task<BaseResult<bool>> DeleteAsync(Guid bookId, Guid metadataId)
        {
            var meta = await _uow.BookMetadata.GetByIdAsync(metadataId);
            if (meta == null || meta.BookId != bookId)
            {
                return BaseResult<bool>.NotFound(
                    $"Không tìm thấy metadata với Id '{metadataId}' cho sách với Id '{bookId}'.");
            }
            _uow.BookMetadata.Delete(meta);
            await _uow.SaveChangesAsync();
            return BaseResult<bool>.Ok(true);
        }
    }
}
