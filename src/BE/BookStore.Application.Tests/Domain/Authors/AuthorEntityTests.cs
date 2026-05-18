using BookStore.Domain.Entities;
using FluentAssertions;

namespace BookStore.Application.Tests.Domain.Authors;

public class AuthorEntityTests
{
    // -----------------------------------------------------------------------
    // Create
    // -----------------------------------------------------------------------

    [Fact]
    public void Create_ShouldReturnAuthor_WithCorrectFields()
    {
        // Act
        var author = Author.Create("Robert C. Martin", "Author of Clean Code");

        // Assert
        author.Id.Should().NotBe(Guid.Empty);
        author.FullName.Should().Be("Robert C. Martin");
        author.Bio.Should().Be("Author of Clean Code");
        author.AvatarUrl.Should().BeNull();
        author.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        author.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldAllowNullBio()
    {
        // Act
        var author = Author.Create("Jane Doe", null);

        // Assert
        author.Bio.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyBookAuthors()
    {
        // Act
        var author = Author.Create("Test Author", null);

        // Assert
        author.BookAuthors.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    [Fact]
    public void Update_ShouldChangeFullNameAndBio()
    {
        // Arrange
        var author = Author.Create("Old Name", "Old bio");

        // Act
        author.Update("New Name", "New bio");

        // Assert
        author.FullName.Should().Be("New Name");
        author.Bio.Should().Be("New bio");
    }

    [Fact]
    public void Update_ShouldAllowClearingBio()
    {
        // Arrange
        var author = Author.Create("Author", "Some bio");

        // Act
        author.Update("Author", null);

        // Assert
        author.Bio.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldRefreshUpdatedAt()
    {
        // Arrange
        var author = Author.Create("Author", null);
        var before = author.UpdatedAt;

        // Act
        author.Update("New Name", null);

        // Assert
        author.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // -----------------------------------------------------------------------
    // SetAvatar
    // -----------------------------------------------------------------------

    [Fact]
    public void SetAvatar_ShouldStoreObjectKey()
    {
        // Arrange
        var author = Author.Create("Author", null);
        const string objectKey = "authors/2024/01/15/abc123.jpg";

        // Act
        author.SetAvatar(objectKey);

        // Assert
        author.AvatarUrl.Should().Be(objectKey);
    }

    [Fact]
    public void SetAvatar_ShouldRefreshUpdatedAt()
    {
        // Arrange
        var author = Author.Create("Author", null);
        var before = author.UpdatedAt;

        // Act
        author.SetAvatar("authors/img.png");

        // Assert
        author.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SetAvatar_ShouldOverwritePreviousAvatar()
    {
        // Arrange
        var author = Author.Create("Author", null);
        author.SetAvatar("authors/old.jpg");

        // Act
        author.SetAvatar("authors/new.jpg");

        // Assert
        author.AvatarUrl.Should().Be("authors/new.jpg");
    }
}
