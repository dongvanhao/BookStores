using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BookStore.Infrastructure.Repository.Common
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly AppDbContext _ctx;

        public IUserRepository Users { get; }
        public IGenericRepository<EmailVerificationToken> EmailVerificationTokens { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IGenericRepository<PasswordResetToken> PasswordResetTokens { get; }

        public IRoleRepository Roles { get; }
        public IPermissionRepository Permissions { get; }
        public IUserRoleRepository UserRoles { get; }
        public IRolePermissionRepository RolePermissions { get; }

        //catalog
        public IBookRepository Books { get; }
        public IPublisherRepository Publishers { get; }
        public IAuthorRepository Author { get; }
        public IBookFileRepository BookFile { get; }
        public IBookFormatRepository BookFormat { get; }
        public IBookImageRepository BookImage { get; }
        public IBookMetadataRepository BookMetadata { get; }
        public ICategoryRepository Category { get; }
        public IBookAuthorRepository BookAuthor { get; }
        public IBookCategoryRepository BookCategory { get; }
        public UnitOfWork(
     AppDbContext ctx,
         IUserRepository users,
         IGenericRepository<EmailVerificationToken> emailVerificationTokens,
         IRefreshTokenRepository refreshTokens,
         IGenericRepository<PasswordResetToken> passwordResetTokens,
         IRoleRepository roles,
         IPermissionRepository permissions,
         IUserRoleRepository userRoles,
         IRolePermissionRepository rolePermissions,
         //catalog
         IBookRepository books,
         IPublisherRepository publisher,
         IBookFormatRepository bookFormat,
         IAuthorRepository author,
         IBookFileRepository bookFile,
         IBookImageRepository bookImage,
         IBookMetadataRepository bookMetadata,
         ICategoryRepository category,
         IBookAuthorRepository bookauthor,
         IBookCategoryRepository bookCategory




           )
        {
            _ctx = ctx;

            Users = users;
            EmailVerificationTokens = emailVerificationTokens;
            RefreshTokens = refreshTokens;
            PasswordResetTokens = passwordResetTokens;

            Roles = roles;
            Permissions = permissions;
            UserRoles = userRoles;
            RolePermissions = rolePermissions;

            Books = books;
            Publishers = publisher;
            BookFormat = bookFormat;
            Author = author;
            BookFile = bookFile;
            BookImage = bookImage;
            BookMetadata = bookMetadata;
            Category = category;
            BookAuthor = bookauthor;
            BookCategory = bookCategory;
        }


        /// <summary>
        /// Lưu thay đổi mà không cần transaction
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _ctx.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Thực thi một hành động trong transaction
        /// </summary>
        public async Task ExecuteTransactionAsync(Func<Task> action)
        {
            var strategy = _ctx.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _ctx.Database.BeginTransactionAsync();
                try
                {
                    await action();
                    await _ctx.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }



        /// <summary>
        /// Dispose đồng bộ
        /// </summary>
        public void Dispose()
        {
            _ctx.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose bất đồng bộ (khuyến nghị EF Core)
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _ctx.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
