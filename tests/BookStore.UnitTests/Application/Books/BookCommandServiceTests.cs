using BookStore.Application.Books.Commands;
using BookStore.Application.Books.Services;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using FluentAssertions;
using Moq;

namespace BookStore.UnitTests.Application.Books;

public class BookCommandServiceTests
{
    private readonly Mock<IBookRepository>      _mockBookRepo;
    private readonly Mock<ICategoryRepository>  _mockCategoryRepo;
    private readonly Mock<IMinioStorageService> _mockMinio;
    private readonly Mock<IUnitOfWork>          _mockUow;
    private readonly BookCommandService         _sut;

    public BookCommandServiceTests()
    {
        _mockBookRepo      = new Mock<IBookRepository>();
        _mockCategoryRepo  = new Mock<ICategoryRepository>();
        _mockMinio         = new Mock<IMinioStorageService>();
        _mockUow           = new Mock<IUnitOfWork>();

        _sut = new BookCommandService(
            _mockBookRepo.Object,
            _mockCategoryRepo.Object,
            _mockMinio.Object,
            _mockUow.Object);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldReturnBookId_WhenValid()
    {
        // Arrange
        var cmd = ValidCreateCmd();
        var category = Category.Create("Tech", null, null);

        _mockBookRepo.Setup(r => r.ExistsByTitleAsync(cmd.Title, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync(cmd.ISBN, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.GetAuthorIdsAsync(It.IsAny<Guid>(), default)).ReturnsAsync([]);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(cmd.CategoryId, default)).ReturnsAsync(category);
        _mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenTitleExists()
    {
        // Arrange
        var cmd = ValidCreateCmd();
        _mockBookRepo.Setup(r => r.ExistsByTitleAsync(cmd.Title, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.TitleExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenISBNExists()
    {
        // Arrange
        var cmd = ValidCreateCmd();
        _mockBookRepo.Setup(r => r.ExistsByTitleAsync(cmd.Title, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync(cmd.ISBN, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.ISBNExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenCategoryNotFound()
    {
        // Arrange
        var cmd = ValidCreateCmd();
        _mockBookRepo.Setup(r => r.ExistsByTitleAsync(cmd.Title, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync(cmd.ISBN, default)).ReturnsAsync(false);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(cmd.CategoryId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.CategoryNotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddBookAuthorRows_WhenAuthorIdsProvided()
    {
        // Arrange
        var authorId1 = Guid.NewGuid();
        var authorId2 = Guid.NewGuid();
        var cmd       = ValidCreateCmd(authorIds: [authorId1, authorId2]);
        var category  = Category.Create("Tech", null, null);

        _mockBookRepo.Setup(r => r.ExistsByTitleAsync(cmd.Title, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync(cmd.ISBN, default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.GetAuthorIdsAsync(It.IsAny<Guid>(), default)).ReturnsAsync([]);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(cmd.CategoryId, default)).ReturnsAsync(category);
        _mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        await _sut.CreateAsync(cmd);

        // Assert
        _mockBookRepo.Verify(r => r.AddBookAuthor(It.IsAny<BookAuthor>()), Times.Exactly(2));
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        var bookId   = Guid.NewGuid();
        var book     = Book.Create("Old Title", null, "ISBN-001", 2020, 100m, 10, Guid.NewGuid());
        var category = Category.Create("Tech", null, null);
        var cmd      = new UpdateBookCommand("New Title", null, "ISBN-002", 2021, 150m, 5, Guid.NewGuid(), []);

        SetBookId(book, bookId);
        _mockBookRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);
        _mockBookRepo.Setup(r => r.ExistsByTitleAsync("New Title", default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync("ISBN-002", default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.GetAuthorIdsAsync(bookId, default)).ReturnsAsync([]);
        _mockCategoryRepo.Setup(r => r.GetByIdAsync(cmd.CategoryId, default)).ReturnsAsync(category);
        _mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(bookId, cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenBookNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockBookRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.UpdateAsync(missingId, new UpdateBookCommand("T", null, "I", 2020, 10m, 0, Guid.NewGuid(), []));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.NotFound");
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenISBNChangedAndAlreadyExists()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = Book.Create("Title", null, "OLD-ISBN", 2020, 100m, 10, Guid.NewGuid());
        SetBookId(book, bookId);
        var cmd = new UpdateBookCommand("Title", null, "NEW-ISBN", 2020, 100m, 10, Guid.NewGuid(), []);

        _mockBookRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);
        _mockBookRepo.Setup(r => r.ExistsByTitleAsync("Title", default)).ReturnsAsync(false);
        _mockBookRepo.Setup(r => r.ExistsByISBNAsync("NEW-ISBN", default)).ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateAsync(bookId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.ISBNExists");
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_WhenBookExists()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = Book.Create("Title", null, "ISBN", 2020, 100m, 10, Guid.NewGuid());
        SetBookId(book, bookId);

        _mockBookRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);
        _mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(bookId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        book.IsDeleted.Should().BeTrue();
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenBookNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockBookRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.DeleteAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.NotFound");
    }

    // -----------------------------------------------------------------------
    // UploadCoverAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UploadCoverAsync_ShouldFail_WhenFileIsNotImage()
    {
        // Arrange
        var file = MockFormFile("document.pdf", "application/pdf", 100);

        // Act
        var result = await _sut.UploadCoverAsync(Guid.NewGuid(), file);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.InvalidCoverFile");
    }

    [Fact]
    public async Task UploadCoverAsync_ShouldFail_WhenFileTooLarge()
    {
        // Arrange
        var file = MockFormFile("cover.jpg", "image/jpeg", 6_000_000); // 6 MB

        // Act
        var result = await _sut.UploadCoverAsync(Guid.NewGuid(), file);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.InvalidCoverFile");
    }

    [Fact]
    public async Task UploadCoverAsync_ShouldFail_WhenBookNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var file      = MockFormFile("cover.jpg", "image/jpeg", 100);
        _mockBookRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Book?)null);

        // Act
        var result = await _sut.UploadCoverAsync(missingId, file);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Book.NotFound");
    }

    [Fact]
    public async Task UploadCoverAsync_ShouldDeleteOldCover_WhenReplacing()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book   = Book.Create("Title", null, "ISBN", 2020, 100m, 10, Guid.NewGuid());
        SetBookId(book, bookId);
        book.SetCover("books/old-key.jpg");

        var file = MockFormFile("new-cover.jpg", "image/jpeg", 100);

        _mockBookRepo.Setup(r => r.GetByIdAsync(bookId, default)).ReturnsAsync(book);
        _mockMinio.Setup(m => m.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                  .Returns(Task.CompletedTask);
        _mockMinio.Setup(m => m.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                  It.IsAny<string>(), It.IsAny<long>(), default))
                  .Returns(Task.CompletedTask);
        _mockMinio.Setup(m => m.GeneratePresignedUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                  .ReturnsAsync("https://minio/presigned-url");
        _mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UploadCoverAsync(bookId, file);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockMinio.Verify(m => m.DeleteAsync("book-images", "books/old-key.jpg", default), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static CreateBookCommand ValidCreateCmd(IReadOnlyList<Guid>? authorIds = null)
        => new("Clean Code", null, "978-0-13-235088-4", 2008, 150_000m, 50, Guid.NewGuid(), authorIds ?? []);

    private static void SetBookId(Book book, Guid id)
        => typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(book, id);

    private static Microsoft.AspNetCore.Http.IFormFile MockFormFile(string fileName, string contentType, long length)
    {
        var mock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return mock.Object;
    }
}
