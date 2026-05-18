using BookStore.Application.Auth;
using BookStore.Application.Auth.IService;
using BookStore.Application.Auth.Services;
using BookStore.Application.Authors.IService;
using BookStore.Application.Authors.Services;
using BookStore.Application.Books.IService;
using BookStore.Application.Books.Services;
using BookStore.Application.Categories.IService;
using BookStore.Application.Categories.Services;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Application.Media.Services;
using BookStore.API.Services;
using BookStore.API.Validators;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository;
using BookStore.Infrastructure.Storage;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Minio;

namespace BookStore.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit           = true;
            options.Password.RequiredLength         = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase       = true;
            options.Password.RequireLowercase       = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryCommandService, CategoryCommandService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddMemoryCache();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        // Media module
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        // Books module
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookCommandService, BookCommandService>();
        services.AddScoped<IBookQueryService, BookQueryService>();
        // Authors module
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IAuthorQueryService, AuthorQueryService>();
        services.AddScoped<IAuthorCommandService, AuthorCommandService>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();
        return services;
    }

    public static IServiceCollection AddMinioServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MinioSettings>(configuration.GetSection("MinioSettings"));

        var settings = configuration.GetSection("MinioSettings").Get<MinioSettings>()
            ?? throw new InvalidOperationException("MinioSettings section is missing.");

        services.AddMinio(client => client
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl));

        services.AddScoped<IMinioStorageService, MinioStorageService>();

        return services;
    }
}
