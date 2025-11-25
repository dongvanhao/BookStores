
using BookStore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Identity
{
    public interface IJwtService 
    {
        string GenerateAccessToken(User user, IEnumerable<string?> roles = null);
        (string token, DateTime expiresAt) GenerateRefreshToken();
        ClaimsPrincipal? ValidateAccessToken(string token);
    }
}
