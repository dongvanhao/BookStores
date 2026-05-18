using BookStore.Application.Authors.Queries;
using BookStore.Application.Authors.Services;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using BookStore.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.UnitTests.Application.Authors;

public class AuthorQueryServiceTests
{
    private readonly Mock<IAuthorRepository> _mockRepo;
    private readonly Mock<IMinioStorageService> _mockMinio;
    private readonly AuthorQueryService _sut;

    public AuthorQueryServiceTests()
    {
        _mockRepo  = new Mock<IAuthorRepository>();
        _mockMinio = new Mock<IMinioStorageService>();

        var minioSettings = Options.Create(new MinioSettings
        {
            Buckets = new Dictionary<string, string> { ["authors"] = "author-avatars" },
            PresignedUrlExpirySeconds = 3600
        });

        _sut = new AuthorQueryService(_mockRepo.Object, _mockMinio.Object, minioSettings);
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAuthorDetail_WhenFound()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("Robert C. Martin", "Author of Clean Code");
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!.SetValue(author, authorId);

        _mockRepo.Setup(r => r.GetByIdWithBooksAsync(authorId, default)).ReturnsAsync(author);

        // Act
        var result = await _sut.GetByIdAsync(authorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(authorId);
        result.Value.FullName.Should().Be("Robert C. Martin");
        result.Value.Bio.Should().Be("Author of Clean Code");
        result.Value.AvatarUrl.Should().BeNull();
        result.Value.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdWithBooksAsync(missingId, default)).ReturnsAsync((Author?)null);

        // Act
        var result = await _sut.GetByIdAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Author.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldGeneratePresignedUrl_WhenAvatarExists()
    {
        // Arrange
        var authorId    = Guid.NewGuid();
        var author      = Author.Create("Author With Avatar", null);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!.SetValue(author, authorId);
        author.SetAvatar("authors/2024/01/abc.jpg");

        const string presignedUrl = "https://minio/authors/2024/01/abc.jpg?X-Amz-Signature=abc";
        _mockRepo.Setup(r => r.GetByIdWithBooksAsync(authorId, default)).ReturnsAsync(author);
        _mockMinio.Setup(m => m.GeneratePresignedUrlAsync("author-avatars", "authors/2024/01/abc.jpg", 3600))
                  .ReturnsAsync(presignedUrl);

        // Act
        var result = await _sut.GetByIdAsync(authorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().Be(presignedUrl);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotCallMinio_WhenNoAvatar()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var author   = Author.Create("No Avatar", null);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!.SetValue(author, authorId);

        _mockRepo.Setup(r => r.GetByIdWithBooksAsync(authorId, default)).ReturnsAsync(author);

        // Act
        await _sut.GetByIdAsync(authorId);

        // Assert
        _mockMinio.Verify(m => m.GeneratePresignedUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // GetPagedAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEmptyPage_WhenNoAuthors()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetQueryable()).Returns(Enumerable.Empty<Author>().AsAsyncQueryable());
        var query = new GetAuthorsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetPagedAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        var authors = new[]
        {
            Author.Create("Robert Martin", null),
            Author.Create("Kent Beck", null),
            Author.Create("Martin Fowler", null)
        }.AsAsyncQueryable();

        _mockRepo.Setup(r => r.GetQueryable()).Returns(authors);
        var query = new GetAuthorsQuery { SearchTerm = "Martin", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetPagedAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(a => a.FullName.Contains("Martin"));
    }
}
