using BookStore.Application.Categories.Commands;
using BookStore.Application.Categories.Services;
using BookStore.Application.Media.Commands;
using BookStore.Application.Media.DTOs;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BookStore.UnitTests.Application.Categories;

public class CategoryCommandServiceTests
{
    private readonly Mock<ICategoryRepository> _mockRepo;
    private readonly Mock<IUnitOfWork>         _mockUow;
    private readonly Mock<IMediaService>       _mockMedia;
    private readonly CategoryCommandService    _sut;

    public CategoryCommandServiceTests()
    {
        _mockRepo  = new Mock<ICategoryRepository>();
        _mockUow   = new Mock<IUnitOfWork>();
        _mockMedia = new Mock<IMediaService>();
        _sut       = new CategoryCommandService(_mockRepo.Object, _mockUow.Object, _mockMedia.Object);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnGuid_WhenValid()
    {
        // Arrange
        var cmd = new CreateCategoryCommand("Fiction", "Fiction books", null);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _mockRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenParentNotFound()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var cmd = new CreateCategoryCommand("Sub", null, parentId);
        _mockRepo.Setup(r => r.GetByIdAsync(parentId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.ParentNotFound", result.Error.Code);
        _mockRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenCategoryNotFound()
    {
        // Arrange
        var id  = Guid.NewGuid();
        var cmd = new UpdateCategoryCommand("Name", null, null);
        _mockRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.UpdateAsync(id, cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenSelfParent()
    {
        // Arrange — parentId set to the same id as the category being updated
        var category = Category.Create("Tech", null);
        var cmd      = new UpdateCategoryCommand("Tech", null, category.Id);

        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.GetDescendantIdsAsync(category.Id, default)).ReturnsAsync([]);

        // Act
        var result = await _sut.UpdateAsync(category.Id, cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.SelfParent", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenCircularReference()
    {
        // Arrange
        var category   = Category.Create("Parent", null);
        var childId    = Guid.NewGuid();
        var cmd        = new UpdateCategoryCommand("Parent", null, childId); // try to set a descendant as parent

        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.GetByIdAsync(childId, default))
            .ReturnsAsync(Category.Create("Child", null, category.Id));
        _mockRepo.Setup(r => r.GetDescendantIdsAsync(category.Id, default))
            .ReturnsAsync([childId]);  // childId is a descendant

        // Act
        var result = await _sut.UpdateAsync(category.Id, cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.CircularReference", result.Error.Code);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenHasChildren()
    {
        // Arrange
        var id       = Guid.NewGuid();
        var category = Category.Create("Tech", null);
        _mockRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.HasChildrenAsync(id, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.HasChildren", result.Error.Code);
        _mockRepo.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenHasBooks()
    {
        // Arrange
        var id       = Guid.NewGuid();
        var category = Category.Create("Tech", null);
        _mockRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.HasChildrenAsync(id, default)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.HasBooksAsync(id, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.HasBooks", result.Error.Code);
        _mockRepo.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
    }

    // ── UploadIconAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UploadIconAsync_ShouldReturnPresignedUrl_WhenSuccess()
    {
        // Arrange
        var category  = Category.Create("Tech", null);
        var mediaId   = Guid.NewGuid();
        var objectKey = "categories/2026/05/08/abc.png";
        var url       = "https://minio/category-icons/abc.png?token=xyz";

        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _mockMedia
            .Setup(m => m.UploadAsync(It.IsAny<UploadMediaCommand>(), default))
            .ReturnsAsync(new MediaDto { Id = mediaId, ObjectKey = objectKey, Url = url });

        var cmd = new UploadCategoryIconCommand
        {
            CategoryId = category.Id,
            File       = new Mock<IFormFile>().Object,
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = await _sut.UploadIconAsync(cmd);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(url, result.Value);
        Assert.Equal(objectKey, category.IconObjectKey);
        Assert.Equal(mediaId, category.IconMediaId);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UploadIconAsync_ShouldFail_WhenCategoryNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync((Category?)null);

        var cmd = new UploadCategoryIconCommand
        {
            CategoryId = categoryId,
            File       = new Mock<IFormFile>().Object,
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = await _sut.UploadIconAsync(cmd);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.NotFound", result.Error.Code);
        _mockMedia.Verify(m => m.UploadAsync(It.IsAny<UploadMediaCommand>(), default), Times.Never);
    }

    // ── DeleteIconAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteIconAsync_ShouldSucceed_WhenIconExists()
    {
        // Arrange
        var category = Category.Create("Tech", null);
        var mediaId  = Guid.NewGuid();
        category.UpdateIcon("categories/abc.png", mediaId);

        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _mockMedia
            .Setup(m => m.DeleteAsync(mediaId, It.IsAny<Guid>(), It.IsAny<bool>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _sut.DeleteIconAsync(category.Id, Guid.NewGuid(), isAdmin: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(category.IconObjectKey);
        Assert.Null(category.IconMediaId);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteIconAsync_ShouldSucceed_WhenNoIcon_Idempotent()
    {
        // Arrange — category without icon
        var category = Category.Create("Tech", null);
        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);

        // Act
        var result = await _sut.DeleteIconAsync(category.Id, Guid.NewGuid(), isAdmin: false);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMedia.Verify(m => m.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), default), Times.Never);
        _mockUow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
