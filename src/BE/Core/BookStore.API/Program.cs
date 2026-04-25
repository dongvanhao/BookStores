using BookStore.API.Extensions;
using BookStore.API.Middlewares;
using BookStore.Application.Options;
using BookStore.Application.Services.Identity;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Data.DataSeeder;
using BookStore.Infrastructure.MinIO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddHealthChecks();

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(opt =>
    {
        opt.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new BadRequestObjectResult(new
            {
                title = "Validation.Failed",
                detail = "One or more validation errors occurred.",
                errors
            });
        };
    });

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Default", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    }
    else
    {
        var clientUrl = builder.Configuration["AppSettings:ClientBaseUrl"] ?? "";
        options.AddPolicy("Default", policy =>
            policy.WithOrigins(clientUrl).AllowAnyMethod().AllowAnyHeader());
    }
});

var jwt = builder.Configuration.GetSection("JwtOptions");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("JwtOptions:Key is missing");

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
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});

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

builder.Services.AddHttpClient();

var app = builder.Build();

await EnsureDatabaseReadyAsync(app);

var minio = app.Services.GetRequiredService<IMinioClient>();
var minioOptions = app.Services.GetRequiredService<IOptions<MinIOOptions>>().Value;

if (!await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(minioOptions.BucketName)))
    await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(minioOptions.BucketName));

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


async Task EnsureDatabaseReadyAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();

    int retries = 5;
    while (retries > 0)
    {
        try
        {
            if (db.Database.GetPendingMigrations().Any())
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Migration applied.");
            }

            await DataSeederRole.SeedRoleAsync(services);
            return;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("SQL server not ready, retrying... ({Retries} left). Error: {Message}", retries, ex.Message);

            if (retries == 0)
            {
                logger.LogError("Migration failed after retries.");
                return;
            }

            await Task.Delay(3000);
        }
    }
}
