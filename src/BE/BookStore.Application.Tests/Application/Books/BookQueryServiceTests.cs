using BookStore.Application.Books.DTOs;
using BookStore.Application.Books.Queries;
using BookStore.Application.Books.Services;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Application.Tests.Helpers;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.Application.Tests.Application.Books;

public class BookQueryServiceTests
{
    private readonly Mock<IBookRepository>      _mockRepo;
    private readonly Mock<IMinioStorageService> _mockMinio;
    private readonly BookQueryService           _sut;

    public BookQueryServiceTests()
    {
        _mockRepo  = new Mock<IBookRepository>();
        _mockMinio = new Mock<IMinioStorageService>();

        var minioOptions = Options.Create(new MinioSettings
        {
            PresignedUrlExpirySeconds = 3600
        });

        _sut = new BookQueryService(_mockRepo.Object, _mockMinio.Object, minioOptions);

        _mockMinio
            .Setup(m => m.GeneratePresignedUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("https://minio/presigned");
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBookDetail_WhenFound()
    {
        // Arrange
        var bookId   = Guid.NewGuid();
        var catId    = Guid.NewGuid();
        var category = CreateCategory("Technology", catId);
        var book     = CreateBook("Clean Code", "978-001", catId, bookId, category);

        _mockRepo.Setup(r => r.GetWithDetailsAsync(bookId, default)).ReturnsAsync(book);

        // Act
        var result = await _sut.GetByIdAsync(bookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(bookId);
        result.Value.Title.Should().Be("Clean Code");
        result.Value.CategoryName.Should().Be("Technology");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetWithDetailsAsync(missingId, default)).ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.GetByIdAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.NotFound");
        result.Error.Type.Should().Be(BookStore.Shared.Results.ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldResolvePresignedUrl_WhenCoverExists()
    {
        // Arrange
        var bookId   = Guid.NewGuid();
        var catId    = Guid.NewGuid();
        var category = CreateCategory("Tech", catId);
        var book     = CreateBook("Title", "ISBN-001", catId, bookId, category);
        book.SetCover("books/cover.jpg");

        _mockRepo.Setup(r => r.GetWithDetailsAsync(bookId, default)).ReturnsAsync(book);

        // Act
        var result = await _sut.GetByIdAsync(bookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CoverUrl.Should().Be("https://minio/presigned");
        _mockMinio.Verify(m => m.GeneratePresignedUrlAsync("book-images", "books/cover.jpg", 3600), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Category CreateCategory(string name, Guid id)
    {
        var cat = Category.Create(name, null, null);
        typeof(BookStore.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(cat, id);
        return cat;
    }

    private static Book CreateBook(
        string title, string isbn, Guid categoryId, Guid bookId, Category category, decimal price = 100m)
    {
        var book = Book.Create(title, null, isbn, 2020, price, 10, categoryId);
        typeof(BookStore.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(book, bookId);
        // Wire up the navigation property so query projections can access Category.Name
        typeof(Book).GetProperty("Category")!.SetValue(book, category);
        return book;
    }
}
