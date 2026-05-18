using BookStore.Application.Auth;
using BookStore.Application.Auth.Commands;
using BookStore.Application.Auth.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.UnitTests.Application.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "super-secret-key-for-testing-must-be-at-least-32-chars!!",
            Issuer = "test",
            Audience = "test",
            AccessExpiryMinutes = 15,
            RefreshExpiryDays = 7
        });

        _sut = new AuthService(
            _mockUserManager.Object,
            _mockRefreshTokenRepo.Object,
            _mockUnitOfWork.Object,
            jwtOptions);
    }

    // -----------------------------------------------------------------------
    // RegisterAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_ShouldReturnTokens_WhenEmailIsNew()
    {
        // Arrange
        var cmd = new RegisterCommand("new@example.com", "Password1!", "New User");

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), cmd.Password))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        _mockRefreshTokenRepo.Setup(r => r.Add(It.IsAny<RefreshToken>()));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.RegisterAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.User.Email.Should().Be(cmd.Email);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenEmailAlreadyExists()
    {
        // Arrange
        var cmd = new RegisterCommand("exists@example.com", "Password1!", "Existing");
        var existingUser = ApplicationUser.Create("Existing", "exists@example.com", "exists@example.com");

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.EmailAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // -----------------------------------------------------------------------
    // LoginAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsValid()
    {
        // Arrange
        var cmd = new LoginCommand("user@example.com", "Password1!");
        var user = ApplicationUser.Create("Test User", "user@example.com", "user@example.com");

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(m => m.CheckPasswordAsync(user, cmd.Password))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(["Customer"]);

        _mockRefreshTokenRepo.Setup(r => r.Add(It.IsAny<RefreshToken>()));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.LoginAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.User.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var cmd = new LoginCommand("nobody@example.com", "Password1!");

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.LoginAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordIncorrect()
    {
        // Arrange
        var cmd = new LoginCommand("user@example.com", "WrongPassword!");
        var user = ApplicationUser.Create("Test User", "user@example.com", "user@example.com");

        _mockUserManager
            .Setup(m => m.FindByEmailAsync(cmd.Email))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(m => m.CheckPasswordAsync(user, cmd.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LoginAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    // -----------------------------------------------------------------------
    // RefreshAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_ShouldRotateTokens_WhenTokenIsActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenStr = "active-refresh-token";
        var activeToken = RefreshToken.Create(userId, tokenStr, DateTime.UtcNow.AddDays(7));
        var user = ApplicationUser.Create("Test User", "user@example.com", "user@example.com");

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(tokenStr, default))
            .ReturnsAsync(activeToken);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(["Customer"]);

        _mockRefreshTokenRepo.Setup(r => r.Add(It.IsAny<RefreshToken>()));
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var cmd = new RefreshTokenCommand(tokenStr);

        // Act
        var result = await _sut.RefreshAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(tokenStr); // rotated
        activeToken.IsRevoked.Should().BeTrue();            // old token revoked
    }

    [Fact]
    public async Task RefreshAsync_ShouldFail_WhenTokenNotFound()
    {
        // Arrange
        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync("missing", default))
            .ReturnsAsync((RefreshToken?)null);

        var cmd = new RefreshTokenCommand("missing");

        // Act
        var result = await _sut.RefreshAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.RefreshTokenNotFound");
    }

    [Fact]
    public async Task RefreshAsync_ShouldFail_WhenTokenExpired()
    {
        // Arrange
        var expiredToken = RefreshToken.Create(Guid.NewGuid(), "expired", DateTime.UtcNow.AddSeconds(-1));

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync("expired", default))
            .ReturnsAsync(expiredToken);

        var cmd = new RefreshTokenCommand("expired");

        // Act
        var result = await _sut.RefreshAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task RefreshAsync_ShouldFail_WhenTokenRevoked()
    {
        // Arrange
        var revokedToken = RefreshToken.Create(Guid.NewGuid(), "revoked", DateTime.UtcNow.AddDays(7));
        revokedToken.Revoke();

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync("revoked", default))
            .ReturnsAsync(revokedToken);

        var cmd = new RefreshTokenCommand("revoked");

        // Act
        var result = await _sut.RefreshAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    // -----------------------------------------------------------------------
    // LogoutAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogoutAsync_ShouldRevokeToken_WhenTokenBelongsToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenStr = "valid-token";
        var token = RefreshToken.Create(userId, tokenStr, DateTime.UtcNow.AddDays(7));

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(tokenStr, default))
            .ReturnsAsync(token);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var cmd = new LogoutCommand(tokenStr, userId);

        // Act
        var result = await _sut.LogoutAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ShouldFail_WhenTokenNotFound()
    {
        // Arrange
        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync("nope", default))
            .ReturnsAsync((RefreshToken?)null);

        var cmd = new LogoutCommand("nope", Guid.NewGuid());

        // Act
        var result = await _sut.LogoutAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.RefreshTokenNotFound");
    }
}
