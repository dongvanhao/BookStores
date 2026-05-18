using BookStore.Application.Categories.DTOs;
using BookStore.Application.Categories.Queries;
using BookStore.Application.Categories.Services;
using BookStore.Application.Media;
using BookStore.Application.Media.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookStore.Application.Tests.Application.Categories;

public class CategoryQueryServiceTests
{
    private readonly Mock<ICategoryRepository> _mockRepo;
    private readonly Mock<IMinioStorageService> _mockMinio;
    private readonly CategoryQueryService _sut;

    public CategoryQueryServiceTests()
    {
        _mockRepo = new Mock<ICategoryRepository>();
        _mockMinio = new Mock<IMinioStorageService>();

        var minioSettings = Options.Create(new MinioSettings
        {
            Buckets = new Dictionary<string, string> { ["Categories"] = "category-icons" },
            PresignedUrlExpirySeconds = 3600
        });

        _sut = new CategoryQueryService(_mockRepo.Object, _mockMinio.Object, minioSettings);
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategoryDto_WhenFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Technology", "Tech books", null);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(category, categoryId);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);

        // Act
        var result = await _sut.GetByIdAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(categoryId);
        result.Value.Name.Should().Be("Technology");
        result.Value.Description.Should().Be("Tech books");
        result.Value.ParentId.Should().BeNull();
        result.Value.ParentName.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.GetByIdAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnParentName_WhenCategoryHasParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = Category.Create("Parent", null, null);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(parent, parentId);

        var child = Category.Create("Child", null, parentId);
        typeof(BookStore.Domain.Entities.Category)
            .GetProperty("Parent")!
            .SetValue(child, parent);

        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(child);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(parentId);
        result.Value.ParentName.Should().Be("Parent");
    }

    // -----------------------------------------------------------------------
    // GetTreeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetTreeAsync_ShouldReturnOnlyRootCategories_WithNestedChildren()
    {
        // Arrange
        var root = Category.Create("Root", null, null);
        var child = Category.Create("Child", null, root.Id); // parentId = root.Id → not a root

        // Manually wire up the hierarchy (Children is ICollection, so Add() works)
        root.Children.Add(child);

        _mockRepo.Setup(r => r.GetAllWithChildrenAsync(default))
                 .ReturnsAsync([root, child]);

        // Act
        var result = await _sut.GetTreeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // only roots
        result.Value[0].Name.Should().Be("Root");
        result.Value[0].Children.Should().HaveCount(1);
        result.Value[0].Children[0].Name.Should().Be("Child");
    }

    [Fact]
    public async Task GetTreeAsync_ShouldReturnEmptyList_WhenNoCategories()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetAllWithChildrenAsync(default)).ReturnsAsync([]);

        // Act
        var result = await _sut.GetTreeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

}
