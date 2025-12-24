using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Identity;
using BookStore.Domain.IRepository.Ordering___Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.IRepository.Common
{
    public interface IUnitOfWork : IDisposable
    {
        // Thêm các repository cụ thể của bạn ở đây
        // Ví dụ: IBookRepository BookRepository { get; }
        // Ví dụ: IShipperRepository ShipperRepository { get; }

        //User & Auth
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IGenericRepository<PasswordResetToken> PasswordResetTokens { get; }

        //Role - Permission
        IRoleRepository Roles { get; }
        IPermissionRepository Permissions { get; }
        IUserRoleRepository UserRoles { get; }
        IRolePermissionRepository RolePermissions { get; }

        //Catalog
        IBookRepository Books { get; }
        IPublisherRepository Publishers { get; }
        IAuthorRepository Author { get; }
        IBookFileRepository BookFile { get; }
        IBookFormatRepository BookFormat { get; }
        IBookImageRepository BookImage { get; }
        IBookMetadataRepository BookMetadata { get; }
        ICategoryRepository Category { get; }
        IBookAuthorRepository BookAuthor { get; }
        IBookCategoryRepository BookCategory { get; }
        //Ordering & Payment
        ICartRepository Cart { get; }
        ICartItemRepository CartItem { get; }
        IOrderRepository Order { get; }
        IOrderItemRepository OrderItem { get; }
        IPaymentTransactionRepository PaymentTransaction { get; }
        IOrderStatusLogRepository OrderStatusLog { get; }
        IOrderHistoryRepository OrderHistory { get; }
        IRefundRepository Refund { get; }
        IPaymentMethodRepository PaymentMethod { get; }
        IGenericRepository<EmailVerificationToken> EmailVerificationTokens { get; }

        


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);// Phương thức SaveChanges cho các nghiệp vụ KHÔNG cần transaction
        Task ExecuteTransactionAsync(Func<Task> action); // Phương thức thực thi các nghiệp vụ có transaction
    }
}
