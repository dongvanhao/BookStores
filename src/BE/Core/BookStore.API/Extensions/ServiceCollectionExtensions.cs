using BookStore.Application.IService.Catalog.Author;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Application.IService.Catalog.Publisher;
using BookStore.Application.IService.Identity;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.IService.Pricing_Inventory;
using BookStore.Application.IService.Storage;
using BookStore.Application.Services.Catalog.Author;
using BookStore.Application.Services.Catalog;
using BookStore.Application.Services.Catalog.Author;
using BookStore.Application.Services.Catalog.Book;
using BookStore.Application.Services.Catalog.Category;
using BookStore.Application.Services.Catalog.Publisher;
using BookStore.Application.Services.Identity;
using BookStore.Application.Services.Ordering_Payment;
using BookStore.Application.Services.Pricing_Inventory;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Domain.IRepository.Ordering_Payment;
using BookStore.Domain.IRepository.Pricing___Inventory;
using BookStore.Infrastructure.MinIO;
using BookStore.Infrastructure.Repository.Catalog;
using BookStore.Infrastructure.Repository.Common;
using BookStore.Infrastructure.Repository.Identity;
using BookStore.Infrastructure.Repository.Ordering_Payment;
using BookStore.Infrastructure.Repository.Pricing___Inventory;

namespace BookStore.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDbSession, DbSession>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Identity
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IUserAddressRepository, UserAddressRepository>();

            // Catalog
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IPublisherRepository, PublisherRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<IBookFileRepository, BookFileRepository>();
            services.AddScoped<IBookImageRepository, BookImageRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBookAuthorRepository, BookAuthorRepository>();
            services.AddScoped<IBookCategoryRepository, BookCategoryRepository>();

            // Ordering & Payment
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartItemRepository, CartItemRepository>();
            services.AddScoped<IOrderStatusLogRepository, OrderStatusLogRepository>();

            // Pricing & Inventory
            services.AddScoped<IDiscountRepository, DiscountRepository>();
            services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
            services.AddScoped<IStockItemRepository, StockItemRepository>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Identity
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IHashingService, BcryptHashingService>();
            services.AddScoped<IEmailSender, EmailSenderFake>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IUserAddressService, UserAddressService>();

            // Catalog
            services.AddScoped<IAuthorService, AuthorService>();
            services.AddScoped<IBookFileService, BookFileService>();
            services.AddScoped<IBookImageService, BookImageService>();
            services.AddScoped<IBookService, BookService>();
            services.AddScoped<IPublisherService, PublisherService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBookAuthorService, BookAuthorService>();
            services.AddScoped<IBookCategoryService, BookCategoryService>();

            // Ordering & Payment
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IOrderStatusLogService, OrderStatusLogService>();
            services.AddScoped<ICartItemService, CartItemService>();

            // Pricing & Inventory
            services.AddScoped<IDiscountService, DiscountService>();
            services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
            services.AddScoped<IStockItemService, StockItemService>();

            // Storage
            services.AddScoped<IStorageService, MinioStorageService>();

            return services;
        }
    }
}
