using BookStore.Domain.Entities;
using BookStore.Shared.Results;

namespace BookStore.UnitTests.Domain.Categories;

public class CategoryTests
{
    private static Category BuildCategory(Guid? id = null)
    {
        var category = Category.Create("Test Category", "Description", parentId: null);
        return category;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldFail_WhenParentIsSelf()
    {
        var category = BuildCategory();

        var result = category.Update("New Name", null, category.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("Category.SelfParent", result.Error.Code);
    }

    [Fact]
    public void Update_ShouldSucceed_WhenParentIdIsNull()
    {
        var category = BuildCategory();

        var result = category.Update("Updated", "Desc", parentId: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", category.Name);
        Assert.Null(category.ParentId);
    }

    // ── UpdateIcon ────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateIcon_ShouldSetIconObjectKey()
    {
        var category = BuildCategory();
        var mediaId  = Guid.NewGuid();

        category.UpdateIcon("categories/2026/05/08/abc.png", mediaId);

        Assert.Equal("categories/2026/05/08/abc.png", category.IconObjectKey);
        Assert.Equal(mediaId, category.IconMediaId);
    }

    [Fact]
    public void UpdateIcon_ShouldOverwriteExistingKey()
    {
        var category  = BuildCategory();
        var newMediaId = Guid.NewGuid();
        category.UpdateIcon("categories/old.png", Guid.NewGuid());

        category.UpdateIcon("categories/new.png", newMediaId);

        Assert.Equal("categories/new.png", category.IconObjectKey);
        Assert.Equal(newMediaId, category.IconMediaId);
    }

    // ── RemoveIcon ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveIcon_ShouldSetIconObjectKeyToNull()
    {
        var category = BuildCategory();
        category.UpdateIcon("categories/2026/05/08/abc.png", Guid.NewGuid());

        category.RemoveIcon();

        Assert.Null(category.IconObjectKey);
        Assert.Null(category.IconMediaId);
    }

    [Fact]
    public void RemoveIcon_ShouldBeIdempotent_WhenAlreadyNull()
    {
        var category = BuildCategory();

        // Should not throw
        category.RemoveIcon();

        Assert.Null(category.IconObjectKey);
    }
}
