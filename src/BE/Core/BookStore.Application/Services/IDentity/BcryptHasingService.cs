using BookStore.Application.IService.Identity;
using BookStore.Domain.IRepository.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.IDentity
{
    public class BcryptHasingService  : IHashingService
    {
        public string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string hashed)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hashed;
        }

        public string HashToken(string tokenPlain)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(tokenPlain);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
