using BookStore.API.Middlewares;
using BookStore.Application.IService.Identity;
using BookStore.Application.Services.Identity;
using BookStore.Application.Services.IDentity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Data.DataSeeder;
using BookStore.Infrastructure.Repository.Common;
using BookStore.Infrastructure.Repository.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IHashingService, BcryptHasingService>();
builder.Services.AddScoped<IEmailSender, EmailSenderFake>();

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
});


var app = builder.Build();

// =====================================
// 8. AUTO MIGRATION + SEEDING
// =====================================
await EnsureDatabaseReadyAsync(app);

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
