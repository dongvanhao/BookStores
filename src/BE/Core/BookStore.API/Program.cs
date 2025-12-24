using BookStore.API.Middlewares;
using BookStore.Application.IService.Catalog.Author;
using BookStore.Application.IService.Catalog.Book;
using BookStore.Application.IService.Catalog.Category;
using BookStore.Application.IService.Catalog.Publisher;
using BookStore.Application.IService.Chatbot;
using BookStore.Application.IService.Identity;
using BookStore.Application.IService.Ordering_Payment;
using BookStore.Application.IService.Storage;
using BookStore.Application.Options;
using BookStore.Application.Services.Catalog;
using BookStore.Application.Services.Catalog.Author;
using BookStore.Application.Services.Catalog.Book;
using BookStore.Application.Services.Catalog.Category;
using BookStore.Application.Services.Catalog.Publisher;
using BookStore.Application.Services.Chatbot;
using BookStore.Application.Services.Identity;
using BookStore.Application.Services.IDentity;
using BookStore.Application.Services.Ordering_Payment;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Domain.IRepository.Ordering___Payment;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Data.DataSeeder;
using BookStore.Infrastructure.MinIO;
using BookStore.Infrastructure.Repository.Catalog;
using BookStore.Infrastructure.Repository.Common;
using BookStore.Infrastructure.Repository.Identity;
using BookStore.Infrastructure.Repository.Ordering___Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// 1. CONNECTION STRING
// =====================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing DefaultConnection");


// =====================================
// 2. DB CONTEXT + RETRY (Docker-friendly)
// =====================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));

builder.Services.AddHealthChecks();

// =====================================
// 3. REPOSITORIES & SERVICES
// =====================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
//Catalog Reportories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IPublisherRepository, PublisherRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IBookFileRepository, BookFileRepository>();
builder.Services.AddScoped<IBookFormatRepository, BookFormatRepository>();
builder.Services.AddScoped<IBookImageRepository, BookImageRepository>();
builder.Services.AddScoped<IBookMetadataRepository, BookMetadataRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBookAuthorRepository, BookAuthorRepository>();
builder.Services.AddScoped<IBookCategoryRepository, BookCategoryRepository>();
//Order&Payment Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IOrderHistoryRepository, OrderHistoryRepository>();
builder.Services.AddScoped<IOrderStatusLogRepository, OrderStatusLogRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IHashingService, BcryptHasingService>();
builder.Services.AddScoped<IEmailSender, EmailSenderFake>();

//CataLog Services
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IBookFileService, BookFileService>();
builder.Services.AddScoped<IBookFormatService, BookFormatService>();
builder.Services.AddScoped<IBookImageService, BookImageService>();
builder.Services.AddScoped<IBookMetadataService, BookMetadataService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IPublisherService, PublisherService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBookAuthorService, BookAuthorService>();
builder.Services.AddScoped<IBookCategoryService, BookCategoryService>();
//Order&Payment Services

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IOrderHistoryService, OrderHistoryService>();
builder.Services.AddScoped<IOrderStatusLogService, OrderStatusLogService>();
builder.Services.AddScoped<ICartItemService,CartItemService>();

//Gemini Service
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();


builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));

// =====================================
// 4. CONTROLLERS (camelCase JSON)
// =====================================
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// =====================================
// 5. CORS
// =====================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// =====================================
// 6. JWT AUTH
// =====================================
var jwt = builder.Configuration.GetSection("JwtOptions");
var jwtKey = jwt["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new Exception("JwtOptions:Key is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// =====================================
// 7. SWAGGER
// =====================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BookStore API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer {token}",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
            },
            Array.Empty<string>()
        }
    });
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});
// =====================================
// MinIO
// =====================================
builder.Services.Configure<MinIOOptions>(builder.Configuration.GetSection("MinIO"));

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MinIOOptions>>().Value;

    return new MinioClient()
        .WithEndpoint(config.Endpoint)
        .WithCredentials(config.AccessKey, config.SecretKey)
        .WithSSL(config.UseSSL)
        .Build();
});
builder.Services.AddScoped<IStorageService, MinioStorageService>();
// Gemini Options
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));

builder.Services.AddHttpClient();

var app = builder.Build();

// =====================================
// 8. AUTO MIGRATION + SEEDING
// =====================================
await EnsureDatabaseReadyAsync(app);
var minio = app.Services.GetRequiredService<IMinioClient>();
var minioOptions = app.Services.GetRequiredService<IOptions<MinIOOptions>>().Value;

bool exists = await minio.BucketExistsAsync(
    new BucketExistsArgs().WithBucket(minioOptions.BucketName));

if (!exists)
{
    await minio.MakeBucketAsync(
        new MakeBucketArgs().WithBucket(minioOptions.BucketName));
}

// =====================================
// 9. PIPELINE
// =====================================
app.MapHealthChecks("/health");

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// =====================================================
// HELPERS — clean, tách biệt rõ ràng
// =====================================================
async Task EnsureDatabaseReadyAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();

    logger.LogInformation("⏳ Checking database...");

    int retries = 5;
    while (retries > 0)
    {
        try
        {
            if (db.Database.GetPendingMigrations().Any())
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("✅ Migration applied.");
            }
            else
            {
                logger.LogInformation("✅ Database already up-to-date.");
            }

            await DataSeederRole.SeedRoleAsync(services);
            logger.LogInformation("🌱 Seed completed.");

            return;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"⚠️ SQL not ready. Retrying... ({retries} left). Error: {ex.Message}");

            if (retries == 0)
            {
                logger.LogError("❌ Migration failed after retries.");
                return; // Không throw để app vẫn start, bạn xem log trong container
            }

            await Task.Delay(3000);
        }
    }
}
