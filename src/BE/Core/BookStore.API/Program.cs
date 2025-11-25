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

// ================== CONNECTION STRING ==================
var connectionString = builder.Configuration.GetConnectionString("UsersDb")
    ?? throw new InvalidOperationException("Missing connection string 'UsersDb'.");

// ================== DB CONTEXT ==================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


// ================== REPOSITORIES + UNIT OF WORK ==================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Identity Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Generic repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));


// ================== AUTH SERVICES ==================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IHashingService, BcryptHasingService>();
builder.Services.AddScoped<IEmailSender, EmailSenderFake>();



// ================== OPTIONS (JWT) ==================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));


// ================== CONTROLLERS + JSON camelCase ==================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });


// ================== CORS ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});


// ================== JWT AUTH ==================
var jwtSection = builder.Configuration.GetSection("JwtOptions");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();


// ================== SWAGGER ==================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BookStore API",
        Version = "v1"
    });

    // Enable JWT Authorization in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Nhập token theo dạng: Bearer {token}",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();
await DataSeederRole.SeedRoleAsync(app.Services);

// ================== MIDDLEWARE PIPELINE ==================

// Swagger cho mọi môi trường (Dev + Prod)
app.UseSwagger();
app.UseSwaggerUI();

// Middleware xử lý lỗi thủ công
app.UseMiddleware<ExceptionMiddleware>();

// KHÔNG dùng HTTPS redirect trong Docker hoặc khi swagger test offline
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
