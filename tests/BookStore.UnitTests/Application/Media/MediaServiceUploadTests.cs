using BookStore.Application.Media;
using BookStore.Application.Media.Commands;
using BookStore.Application.Media.IService;
using BookStore.Application.Media.Services;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.UnitTests.Application.Media;

public class MediaServiceUploadTests
{
    private readonly Mock<IMediaRepository>      _mockRepo;
    private readonly Mock<IUnitOfWork>           _mockUow;
    private readonly Mock<IMinioStorageService>  _mockStorage;
    private readonly MediaService                _sut;
    private readonly MinioSettings               _settings;

    public MediaServiceUploadTests()
    {
        _mockRepo    = new Mock<IMediaRepository>();
        _mockUow     = new Mock<IUnitOfWork>();
        _mockStorage = new Mock<IMinioStorageService>();

        _settings = new MinioSettings
        {
            Buckets = new Dictionary<string, string>
            {
                { "Books",   "books" },
                { "Authors", "authors" },
                { "Users",   "users" }
            },
            AllowedMimeTypes      = ["image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf"],
            MaxFileSizeBytes      = 10_485_760,
            PresignedUrlExpirySeconds = 3600
        };

        var options = Options.Create(_settings);
        _sut = new MediaService(_mockRepo.Object, _mockUow.Object, _mockStorage.Object, options);
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnMediaDto_WhenSuccess()
    {
        // Arrange
        var file   = CreateMockFile("photo.jpg", "image/jpeg", 500_000);
        var cmd    = BuildCommand(file, "books");
        var presigned = "https://minio/books/test.jpg?sig=abc";

        _mockStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStorage
            .Setup(s => s.GeneratePresignedUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(presigned);

        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.UploadAsync(cmd);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(presigned, result.Value.Url);
        Assert.Equal("image", result.Value.Type);
        Assert.Equal("image/jpeg", result.Value.MimeType);
        _mockRepo.Verify(r => r.Add(It.IsAny<BookStore.Domain.Entities.Media>()), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenInvalidMimeType()
    {
        // Arrange
        var file = CreateMockFile("virus.exe", "application/octet-stream", 1_000);
        var cmd  = BuildCommand(file, "books");

        // Act
        var result = await _sut.UploadAsync(cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Media.InvalidFileType", result.Error.Code);
        _mockStorage.Verify(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.Add(It.IsAny<BookStore.Domain.Entities.Media>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenFileTooLarge()
    {
        // Arrange
        var file = CreateMockFile("big.jpg", "image/jpeg", 20_000_000); // 20 MB > 10 MB limit
        var cmd  = BuildCommand(file, "books");

        // Act
        var result = await _sut.UploadAsync(cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Media.FileTooLarge", result.Error.Code);
    }

    [Fact]
    public async Task UploadAsync_ShouldFail_WhenMinioThrows()
    {
        // Arrange
        var file = CreateMockFile("photo.jpg", "image/jpeg", 500_000);
        var cmd  = BuildCommand(file, "books");

        _mockStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MinIO connection refused"));

        // Act
        var result = await _sut.UploadAsync(cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Media.UploadFailed", result.Error.Code);
        _mockRepo.Verify(r => r.Add(It.IsAny<BookStore.Domain.Entities.Media>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenOwner()
    {
        // Arrange
        var ownerId  = Guid.NewGuid();
        var mediaId  = Guid.NewGuid();
        var media    = BookStore.Domain.Entities.Media.Create(
            "books/2026/05/01/test.jpg", null, "books", "books",
            "test.jpg", "image/jpeg", 100_000, null, null,
            BookStore.Domain.Enums.MediaType.Image, ownerId);

        _mockRepo.Setup(r => r.GetByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);
        _mockStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(mediaId, ownerId, isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepo.Verify(r => r.Remove(media), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenNotOwnerNotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var media   = BookStore.Domain.Entities.Media.Create(
            "books/2026/05/01/test.jpg", null, "books", "books",
            "test.jpg", "image/jpeg", 100_000, null, null,
            BookStore.Domain.Enums.MediaType.Image, ownerId);

        _mockRepo.Setup(r => r.GetByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(media);

        // Act
        var result = await _sut.DeleteAsync(mediaId, Guid.NewGuid(), isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        _mockStorage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var media   = BookStore.Domain.Entities.Media.Create(
            "books/2026/05/01/test.jpg", null, "books", "books",
            "test.jpg", "image/jpeg", 100_000, null, null,
            BookStore.Domain.Enums.MediaType.Image, ownerId);

        _mockRepo.Setup(r => r.GetByIdAsync(mediaId, It.IsAny<CancellationToken>())).ReturnsAsync(media);
        _mockStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(mediaId, Guid.NewGuid(), isAdmin: true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenNotFound()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookStore.Domain.Entities.Media?)null);

        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    // --- Helpers ---

    private static IFormFile CreateMockFile(string fileName, string contentType, long size)
    {
        var mockFile = new Mock<IFormFile>();
        var stream   = new MemoryStream(new byte[size]);

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(size);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        return mockFile.Object;
    }

    private static UploadMediaCommand BuildCommand(IFormFile file, string module) =>
        new() { File = file, Module = module, UploadedBy = Guid.NewGuid() };
}
