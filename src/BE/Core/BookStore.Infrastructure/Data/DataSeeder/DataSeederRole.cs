using BookStore.Application.IService.Identity;
using BookStore.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Data.DataSeeder
{
    public class DataSeederRole
    {
        public static async Task SeedRoleAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Kiểm tra xem đã có Role nào chưa, nếu chưa thì thêm mới
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
            {
                new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrator with full access" },
                new Role { Id = Guid.NewGuid(), Name = "Customer", Description = "Regular customer" },
                new Role { Id = Guid.NewGuid(), Name = "Staff", Description = "Store staff" }
            };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
            var adminEmail = "admin@bookstore.com";
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (adminUser == null)
            {
                // Lấy service Hashing ra để dùng (nếu bạn đã đăng ký nó trong DI Container)
                var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

                // Hoặc nếu không muốn dùng service, bạn có thể hardcode hash ở đây (nhưng nên dùng service cho đồng bộ)
                var passwordHash = hashingService.HashPassword("Admin@123");

                var newAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    PasswordHash = passwordHash,
                    EmailConfirmed = true,
                    IsActive = true
                };

                await context.Users.AddAsync(newAdmin);

                // 3. Gán Role Admin cho User này
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    await context.UserRoles.AddAsync(new UserRole
                    {
                        UserId = newAdmin.Id,
                        RoleId = adminRole.Id
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
