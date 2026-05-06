using BookStore.Application.Categories.Commands;
using BookStore.Application.Categories.IService;
using BookStore.Application.Categories.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;
using FluentAssertions;
using Moq;

namespace BookStore.Application.Tests.Application.Categories;

public class CategoryCommandServiceTests
{
    private readonly Mock<ICategoryRepository> _mockRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CategoryCommandService _sut;

    public CategoryCommandServiceTests()
    {
        _mockRepo = new Mock<ICategoryRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _sut = new CategoryCommandService(_mockRepo.Object, _mockUnitOfWork.Object);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldReturnGuid_WhenValidRootCategory()
    {
        // Arrange
        var cmd = new CreateCategoryCommand("Technology", "Tech books", null);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnGuid_WhenValidChildCategory()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = Category.Create("Parent", null, null);
        var cmd = new CreateCategoryCommand("Child", null, parentId);

        _mockRepo.Setup(r => r.GetByIdAsync(parentId, default)).ReturnsAsync(parent);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenParentNotFound()
    {
        // Arrange
        var missingParentId = Guid.NewGuid();
        var cmd = new CreateCategoryCommand("Child", null, missingParentId);

        _mockRepo.Setup(r => r.GetByIdAsync(missingParentId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.CreateAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.ParentNotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Old Name", null, null);
        var cmd = new UpdateCategoryCommand("New Name", "Updated desc", null);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateAsync(categoryId, cmd);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenCategoryNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var cmd = new UpdateCategoryCommand("Name", null, null);

        _mockRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.UpdateAsync(missingId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenSelfParent()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Name", null, null);
        // Force the Id to match via reflection (Category.Id is set via factory)
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(category, categoryId);

        var cmd = new UpdateCategoryCommand("Name", null, categoryId); // parentId == id

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category); // parent check
        _mockRepo.Setup(r => r.GetDescendantIdsAsync(categoryId, default)).ReturnsAsync([]);

        // Act
        var result = await _sut.UpdateAsync(categoryId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.SelfParent");
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenCircularReference()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var category = Category.Create("Parent", null, null);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(category, categoryId);

        var child = Category.Create("Child", null, categoryId);
        typeof(BookStore.Domain.Common.BaseEntity)
            .GetProperty("Id")!
            .SetValue(child, childId);

        var cmd = new UpdateCategoryCommand("Parent", null, childId); // setting child as parent → circular

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.GetByIdAsync(childId, default)).ReturnsAsync(child); // parent exists check
        _mockRepo.Setup(r => r.GetDescendantIdsAsync(categoryId, default))
                 .ReturnsAsync([childId]); // childId is a descendant

        // Act
        var result = await _sut.UpdateAsync(categoryId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.CircularReference");
    }

    [Fact]
    public async Task UpdateAsync_ShouldFail_WhenNewParentNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var missingParentId = Guid.NewGuid();
        var category = Category.Create("Name", null, null);
        var cmd = new UpdateCategoryCommand("Name", null, missingParentId);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.GetByIdAsync(missingParentId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.UpdateAsync(categoryId, cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.ParentNotFound");
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenCategoryIsLeafAndEmpty()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Leaf", null, null);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.HasChildrenAsync(categoryId, default)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.HasBooksAsync(categoryId, default)).ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepo.Verify(r => r.Remove(category), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenCategoryNotFound()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Category?)null);

        // Act
        var result = await _sut.DeleteAsync(missingId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenHasChildren()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Parent", null, null);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.HasChildrenAsync(categoryId, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.HasChildren");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenHasBooks()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Used", null, null);

        _mockRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _mockRepo.Setup(r => r.HasChildrenAsync(categoryId, default)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.HasBooksAsync(categoryId, default)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(categoryId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.HasBooks");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
