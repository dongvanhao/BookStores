using BookStore.Domain.Entities;
using FluentAssertions;

namespace BookStore.Application.Tests.Domain.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void Revoke_ShouldSetIsRevokedTrue()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddDays(7));

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenRevoked()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenExpired()
    {
        // Arrange — expiry in the past
        var token = RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddSeconds(-1));

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenValidAndNotRevoked()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddDays(7));

        // Act & Assert
        token.IsActive.Should().BeTrue();
    }
}
