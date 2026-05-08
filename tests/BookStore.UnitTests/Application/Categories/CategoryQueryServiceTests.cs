using BookStore.Application.Categories.Services;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.UnitTests.Application.Categories;

public class CategoryQueryServiceTests
{
    private readonly Mock<ICategoryRepository>  _mockRepo;
    private readonly Mock<IMinioStorageService> _mockStorage;
    private readonly CategoryQueryService       _sut;

    public CategoryQueryServiceTests()
    {
        _mockRepo    = new Mock<ICategoryRepository>();
        _mockStorage = new Mock<IMinioStorageService>();

        var settings = new MinioSettings
        {
            Buckets                  = new() { ["Categories"] = "category-icons" },
            PresignedUrlExpirySeconds = 3600
        };

        _sut = new CategoryQueryService(
            _mockRepo.Object,
            _mockStorage.Object,
            Options.Create(settings));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var category = Category.Create("Fiction", "Good books", null);
        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);

        // Act
        var result = await _sut.GetByIdAsync(category.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id,   result.Value.Id);
        Assert.Equal("Fiction",     result.Value.Name);
        Assert.Equal("Good books",  result.Value.Description);
        Assert.Null(result.Value.IconUrl);  // no icon set
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WithPresignedUrl_WhenIconExists()
    {
        // Arrange
        var category = Category.Create("Fiction", null, null);
        category.UpdateIcon("categories/abc.png", Guid.NewGuid());

        _mockRepo.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);
        _mockStorage
            .Setup(s => s.GeneratePresignedUrlAsync("category-icons", "categories/abc.png", 3600))
            .ReturnsAsync("https://minio/category-icons/abc.png?token=xyz");

        // Act
        var result = await _sut.GetByIdAsync(category.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("https://minio/category-icons/abc.png?token=xyz", result.Value.IconUrl);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Category.NotFound", result.Error.Code);
    }

    // ── GetTreeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTreeAsync_ShouldReturnOnlyRootCategories()
    {
        // Arrange — 2 roots, 1 child under root1
        var root1  = Category.Create("Root1", null, null);
        var root2  = Category.Create("Root2", null, null);
        var child1 = Category.Create("Child1", null, root1.Id);

        // EF Core identity fix-up: add child to root1.Children
        root1.Children.Add(child1);

        _mockRepo.Setup(r => r.GetAllWithChildrenAsync(default))
            .ReturnsAsync([root1, root2, child1]);

        // Act
        var result = await _sut.GetTreeAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);  // only roots
        Assert.Contains(result.Value, t => t.Id == root1.Id);
        Assert.Contains(result.Value, t => t.Id == root2.Id);

        var root1Dto = result.Value.First(t => t.Id == root1.Id);
        Assert.Single(root1Dto.Children);
        Assert.Equal(child1.Id, root1Dto.Children[0].Id);
    }

    [Fact]
    public async Task GetSubtreeAsync_ShouldReturnNotFound_WhenIdNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetAllWithChildrenAsync(default)).ReturnsAsync([]);

        // Act
        var result = await _sut.GetSubtreeAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Category.NotFound", result.Error.Code);
    }
}
