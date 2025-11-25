using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Identity;
using BookStore.Infrastructure.Data;
using BookStore.Infrastructure.Repository.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repository.Identity
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly AppDbContext _context;
        public RefreshTokenRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
            await _context.RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        public async Task<IEnumerable<RefreshToken>> GetActiveTokenforUserAsync(Guid userId) =>
            await _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.Revoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
}
