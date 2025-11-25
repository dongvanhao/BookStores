using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Infrastructure.Data;
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

        public UnitOfWork(
     AppDbContext ctx,
         IUserRepository users,
         IGenericRepository<EmailVerificationToken> emailVerificationTokens,
         IRefreshTokenRepository refreshTokens,
         IGenericRepository<PasswordResetToken> passwordResetTokens,
         IRoleRepository roles,
         IPermissionRepository permissions,
         IUserRoleRepository userRoles,
         IRolePermissionRepository rolePermissions
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
        public async Task ExcuteTransactionAsync(Func<Task> action)
        {
            // Begin transaction
            await using var transaction = await _ctx.Database.BeginTransactionAsync();
            try
            {
                await action(); // Thực thi các repository thao tác dữ liệu
                await _ctx.SaveChangesAsync(); // Lưu tất cả thay đổi
                await transaction.CommitAsync(); // Commit transaction
            }
            catch
            {
                await transaction.RollbackAsync(); // Rollback nếu có lỗi
                throw;
            }
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
