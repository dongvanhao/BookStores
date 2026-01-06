using BookStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookStore.Infrastructure.Data
{
    /// <summary>
    /// Design-time DbContext Factory
    /// 👉 Chỉ dùng cho EF Core CLI (add/remove/update migration)
    /// 👉 KHÔNG phụ thuộc Program.cs, JWT, MinIO, Seed, ENV
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // 🔥 Connection string CHỈ dành cho migration
            // 👉 KHÔNG dùng ở runtime
            var connectionString =
                "Server=localhost,1433;" +
                "Database=BookStoreDb;" +
                "User Id=sa;" +
                "Password=Str0ngP@ssw0rd_2025!;" +
                "TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(
                connectionString,
                sql =>
                {
                    // (optional) nếu bạn có nhiều project migration
                    sql.MigrationsAssembly("BookStore.Infrastructure");
                });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
