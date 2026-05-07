using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category>     Categories    => Set<Category>();
    public DbSet<Author>       Authors       => Set<Author>();
    public DbSet<Book>         Books         => Set<Book>();
    public DbSet<Order>        Orders        => Set<Order>();
    public DbSet<OrderItem>    OrderItems    => Set<OrderItem>();
    public DbSet<Review>       Reviews       => Set<Review>();
    public DbSet<Media>        Media         => Set<Media>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Auto-discover tất cả IEntityTypeConfiguration<T> trong assembly này
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Đổi tên bảng Identity mặc định cho gọn
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
