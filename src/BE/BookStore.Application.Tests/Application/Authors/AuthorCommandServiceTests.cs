using BookStore.Application.Authors.Commands;
using BookStore.Application.Authors.Services;
using BookStore.Application.Media.Commands;
using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MediaEntity = BookStore.Domain.Entities.Media;

namespace BookStore.Application.Tests.Application.Authors;

public class AuthorCommandServiceTests
{
    private readonly Mock<IAuthorRepository>  _mockAuthorRepo;
    private readonly Mock<IMediaService>      _mockMediaService;
    private readonly Mock<IMediaRepository>   _mockMediaRepo;
    private readonly Mock<IUnitOfWork>        _mockUnitOfWork;
    private readonly AuthorCommandService     _sut;

    public AuthorCommandServiceTests()
    {
        _mockAuthorRepo   = new Mock<IAuthorRepository>();
        _mockMediaService = new Mock<IMediaService>();
        _mockMediaRepo    = new Mock<IMediaRepository>();
        _mockUnitOfWork   = new Mock<IUnitOfWork>();

        _sut = new AuthorCommandService(
            _mockAuthorRepo.Object,
            _mockMediaService.Object,
            _mockMediaRepo.Object,
            _mockUnitOfWork.Object);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldReturnGuid_WhenValidCommand()
    {
        // Arrange
        var cmd = new CreateAuthorCommand("Robert C. Martin", "Author of Clean Code");
        _mockAuthorRepo.Setup(r => r.ExistsByFullNameAsync(cmd.FullName, default)).ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _mockAuthorRepo.Verify(r => r.Add(It.Is<Author>(a => a.FullName == cmd.FullName)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenFullNameAlreadyExists()
    {
        // Arrange
        var cmd = new CreateAuthorCommand("Robert C. Martin", null);
        _mockAuthorRepo.Setup(r => r.ExistsByFullNameAsync(cmd.FullName, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.FullNameExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _mockAuthorRepo.Verify(r => r.Add(It.IsAny<Author>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Old Name", "Old bio");
        var cmd      = new UpdateAuthorCommand("New Name", "New bio");

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockAuthorRepo.Setup(r => r.ExistsByFullNameAsync("New Name", default)).ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(authorId, cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        author.FullName.Should().Be("New Name");
        author.Bio.Should().Be("New bio");
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenAuthorNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockAuthorRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Author?)null);

        // Act
        var result = await _sut.UpdateAsync(missingId, new UpdateAuthorCommand("Name", null));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenNewFullNameAlreadyExists()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Old Name", null);
        var cmd      = new UpdateAuthorCommand("Taken Name", null);

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockAuthorRepo.Setup(r => r.ExistsByFullNameAsync("Taken Name", default)).ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateAsync(authorId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.FullNameExists");
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenSameFullNameUnchanged()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Same Name", null);
        var cmd      = new UpdateAuthorCommand("Same Name", "Updated bio");

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(authorId, cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockAuthorRepo.Verify(r => r.ExistsByFullNameAsync(It.IsAny<string>(), default), Times.Never);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenNoLinkedBooks()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Author", null);

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockAuthorRepo.Setup(r => r.HasBooksAsync(authorId, default)).ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(authorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockAuthorRepo.Verify(r => r.Remove(author), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenAuthorHasLinkedBooks()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Author", null);

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockAuthorRepo.Setup(r => r.HasBooksAsync(authorId, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(authorId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.HasBooks");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _mockAuthorRepo.Verify(r => r.Remove(It.IsAny<Author>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenAuthorNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockAuthorRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Author?)null);

        // Act
        var result = await _sut.DeleteAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.NotFound");
    }

    // -----------------------------------------------------------------------
    // UploadAvatarAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UploadAvatarAsync_ShouldFail_WhenAuthorNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockAuthorRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Author?)null);

        // Act
        var result = await _sut.UploadAvatarAsync(missingId, MockFile("photo.jpg", 1024), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.NotFound");
    }

    [Fact]
    public async Task UploadAvatarAsync_ShouldFail_WhenFileTooLarge()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default))
                       .ReturnsAsync(Author.Create("Author", null));

        // Act
        var result = await _sut.UploadAvatarAsync(authorId, MockFile("photo.jpg", 6_000_000), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.AvatarTooLarge");
    }

    [Fact]
    public async Task UploadAvatarAsync_ShouldFail_WhenInvalidFormat()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default))
                       .ReturnsAsync(Author.Create("Author", null));

        // Act
        var result = await _sut.UploadAvatarAsync(authorId, MockFile("photo.gif", 1024), Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.AvatarInvalidFormat");
    }

    [Fact]
    public async Task UploadAvatarAsync_ShouldDeleteOldAvatar_WhenAvatarExists()
    {
        // Arrange
        var authorId   = Guid.NewGuid();
        var uploadedBy = Guid.NewGuid();
        var author     = Author.Create("Author", null);
        author.SetAvatar("authors/old.jpg");

        var oldMedia = MediaEntity.Create(
            "authors/old.jpg", null, "author-avatars", "authors",
            "old.jpg", "image/jpeg", 1024, null, null,
            MediaType.Image, uploadedBy);

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockMediaRepo.Setup(r => r.GetByObjectKeyAsync("authors/old.jpg", default)).ReturnsAsync(oldMedia);
        _mockMediaService.Setup(m => m.DeleteAsync(oldMedia.Id, uploadedBy, true, default))
                         .ReturnsAsync(Result.Success());
        _mockMediaService.Setup(m => m.UploadAsync(It.IsAny<UploadMediaCommand>(), default))
                         .ReturnsAsync(new MediaDto
                         {
                             Id = Guid.NewGuid(), ObjectKey = "authors/new.jpg",
                             Url = "https://minio/new.jpg"
                         });
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UploadAvatarAsync(authorId, MockFile("photo.jpg", 1024), uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMediaService.Verify(m => m.DeleteAsync(oldMedia.Id, uploadedBy, true, default), Times.Once);
    }

    [Fact]
    public async Task UploadAvatarAsync_ShouldNotDeleteOldAvatar_WhenNoExistingAvatar()
    {
        // Arrange
        var authorId   = Guid.NewGuid();
        var uploadedBy = Guid.NewGuid();
        var author     = Author.Create("Author", null); // AvatarUrl is null

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockMediaService.Setup(m => m.UploadAsync(It.IsAny<UploadMediaCommand>(), default))
                         .ReturnsAsync(new MediaDto
                         {
                             Id = Guid.NewGuid(), ObjectKey = "authors/new.jpg",
                             Url = "https://minio/new.jpg"
                         });
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UploadAvatarAsync(authorId, MockFile("photo.jpg", 1024), uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMediaService.Verify(m => m.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), default), Times.Never);
    }

    [Fact]
    public async Task UploadAvatarAsync_ShouldReturnPresignedUrl_WhenSuccess()
    {
        // Arrange
        var authorId   = Guid.NewGuid();
        var uploadedBy = Guid.NewGuid();
        var author     = Author.Create("Author", null);

        _mockAuthorRepo.Setup(r => r.GetByIdAsync(authorId, default)).ReturnsAsync(author);
        _mockMediaService.Setup(m => m.UploadAsync(It.IsAny<UploadMediaCommand>(), default))
                         .ReturnsAsync(new MediaDto
                         {
                             Id = Guid.NewGuid(), ObjectKey = "authors/2024/01/abc.jpg",
                             Url = "https://minio/author-avatars/abc.jpg?X-Amz-Signature=xyz"
                         });
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UploadAvatarAsync(authorId, MockFile("photo.jpg", 1024), uploadedBy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://minio/author-avatars/abc.jpg?X-Amz-Signature=xyz");
        author.AvatarUrl.Should().Be("authors/2024/01/abc.jpg"); // ObjectKey stored, not presigned URL
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IFormFile MockFile(string fileName, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(length);
        return mock.Object;
    }
}
