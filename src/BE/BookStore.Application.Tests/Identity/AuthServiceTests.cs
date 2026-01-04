using BookStore.Application.Dtos.IdentityDto;
using BookStore.Application.IService.Identity;
using BookStore.Application.Services.IDentity;
using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using BookStore.Shared.Common;
using BookStore.Shared.Errors;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Tests.Identity
{
    public class AuthServiceTests
    {// 1️ Mock UoW và các dependency
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IUserRoleRepository> _userRoles = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
        private readonly Mock<IGenericRepository<PasswordResetToken>> _passwordResetTokens = new();

        private readonly Mock<IJwtService> _jwt = new();
        private readonly Mock<IHashingService> _hashing = new();
        private readonly Mock<IEmailSender> _email = new();

        // 2️ Service cần test
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            //  GÁN repository con vào UoW
            _uow.Setup(x => x.Users).Returns(_users.Object);
            _uow.Setup(x => x.UserRoles).Returns(_userRoles.Object);
            _uow.Setup(x => x.RefreshTokens).Returns(_refreshTokens.Object);
            _uow.Setup(x => x.PasswordResetTokens).Returns(_passwordResetTokens.Object);
            _uow
            .Setup(x => x.ExecuteTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(async action =>
            {
            // CHẠY NGHIỆP VỤ BÊN TRONG TRANSACTION
            await action();
            });

            // SaveChanges luôn OK
            _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new AuthService(
                _uow.Object,
                _jwt.Object,
                _hashing.Object,
                _email.Object
            );
        }
        [Fact]
        public async Task Login_WrongPassword_ShouldFail()
        {
            // ARRANGE
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@mail.com",
                PasswordHash = "hashed-password"
            };

            _users
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _hashing
                .Setup(x => x.VerifyPassword("wrong", user.PasswordHash))
                .Returns(false);

            // ACT
            var result = await _service.LoginAsync(
                new AuthDto.LoginDto("test@mail.com", "wrong"),
                "127.0.0.1"
            );

            // ASSERT
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be(AuthErrors.InvalidCredentials.Code);
        }
        [Fact]
        public async Task Login_Success_ShouldReturnAccessAndRefreshToken()
        {
            // ARRANGE
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@mail.com",
                PasswordHash = "hashed"
            };

            _users
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _hashing
                .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            _userRoles
                .Setup(x => x.GetRolesByUserId(user.Id))
                .ReturnsAsync(new[] { new Role { Name = "Customer" } });

            _jwt
                .Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>()))
                .Returns("access-token");

            _jwt
                .Setup(x => x.GenerateRefreshToken())
                .Returns(("refresh-token", DateTime.UtcNow.AddDays(7)));

            // ACT
            var result = await _service.LoginAsync(
                new AuthDto.LoginDto("test@mail.com", "123"),
                "127.0.0.1"
            );

            // ASSERT
            result.IsSuccess.Should().BeTrue();
            result.Value!.AccessToken.Should().Be("access-token");
            result.Value.RefreshToken.Should().Be("refresh-token");
        }
        [Fact]
        public async Task Logout_NullToken_ShouldFail()
        {
            var result = await _service.LogoutAsync("");

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Validation);
        }
        [Fact]
        public async Task Logout_ValidToken_ShouldRevoke()
        {
            var refreshToken = "plain-token";
            var hash = "hashed-token";

            var entity = new RefreshToken
            {
                TokenHash = hash,
                Revoked = false
            };

            _hashing.Setup(x => x.HashToken(refreshToken))
                    .Returns(hash);

            _refreshTokens
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(entity);

            var result = await _service.LogoutAsync(refreshToken);

            result.IsSuccess.Should().BeTrue();
            entity.Revoked.Should().BeTrue();
        }
        [Fact]
        public async Task RefreshToken_Null_ShouldFail()
        {
            var result = await _service.RefreshTokenAsync("", "127.0.0.1");

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Validation);
        }
        [Fact]
        public async Task RefreshToken_NotFound_ShouldFail()
        {
            _hashing.Setup(x => x.HashToken("token"))
                    .Returns("hash");

            _refreshTokens
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken?)null);

            var result = await _service.RefreshTokenAsync("token", "127.0.0.1");

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Unauthorized);
        }
        [Fact]
        public async Task RefreshToken_Success_ShouldRotateToken()
        {
            var userId = Guid.NewGuid();
            var oldToken = new RefreshToken
            {
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                Revoked = false
            };

            var user = new User
            {
                Id = userId,
                Email = "test@mail.com"
            };

            _hashing.Setup(x => x.HashToken("old"))
                    .Returns("old-hash");

            _refreshTokens
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(oldToken);
            _refreshTokens
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);


            _users
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRoles
                .Setup(x => x.GetRolesByUserId(userId))
                .ReturnsAsync(new[] { new Role { Name = "Customer" } });

            _jwt.Setup(x => x.GenerateRefreshToken())
                .Returns(("new-plain", DateTime.UtcNow.AddDays(7)));

            _jwt.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>()))
                .Returns("access-token");

            var result = await _service.RefreshTokenAsync("old", "127.0.0.1");

            result.IsSuccess.Should().BeTrue();
            oldToken.Revoked.Should().BeTrue();
            result.Value!.AccessToken.Should().Be("access-token");
        }
        [Fact]
        public async Task ResetPassword_InvalidInput_ShouldFail()
        {
            var result = await _service.ResetPasswordAsync(null!);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Validation);
        }
        [Fact]
        public async Task ResetPassword_TokenNotFound_ShouldFail()
        {
            _hashing.Setup(x => x.HashToken("token"))
                    .Returns("hash");

            _passwordResetTokens
                .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>()))
                .ReturnsAsync((PasswordResetToken?)null);

            var dto = new AuthDto.ResetPasswordDto(
                Guid.NewGuid(),
                "token",
                "newPass"
            );

            var result = await _service.ResetPasswordAsync(dto);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Type.Should().Be(ErrorType.Validation);
        }
        [Fact]
        public async Task ResetPassword_Success_ShouldUpdatePassword()
        {
            var user = new User { Id = Guid.NewGuid() };

            var token = new PasswordResetToken
            {
                User = user,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Used = false
            };

            _hashing.Setup(x => x.HashToken("token"))
                    .Returns("hash");

            _hashing.Setup(x => x.HashPassword("newPass"))
                    .Returns("hashed-pass");
            _users
                    .Setup(x => x.Update(It.IsAny<User>()));

            _passwordResetTokens
                    .Setup(x => x.Update(It.IsAny<PasswordResetToken>()));

            _passwordResetTokens
                    .Setup(x => x.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>()))
                    .ReturnsAsync(token);

            var dto = new AuthDto.ResetPasswordDto(
                user.Id,
                "token",
                "newPass"
            );

            var result = await _service.ResetPasswordAsync(dto);

            result.IsSuccess.Should().BeTrue();
            token.Used.Should().BeTrue();
            user.PasswordHash.Should().Be("hashed-pass");
        }

    }
}
