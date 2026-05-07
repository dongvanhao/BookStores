using BookStore.Domain.Enums;
using BookStore.Shared.Results;
using MediaEntity = BookStore.Domain.Entities.Media;

namespace BookStore.UnitTests.Domain.Media;

public class MediaTests
{
    private static MediaEntity BuildMedia(Guid? uploadedBy = null) =>
        MediaEntity.Create(
            objectKey: "books/2026/05/01/test.jpg",
            thumbnailKey: null,
            bucketName: "books",
            module: "books",
            originalFileName: "test.jpg",
            mimeType: "image/jpeg",
            size: 100_000,
            width: 1920,
            height: 1080,
            type: MediaType.Image,
            uploadedBy: uploadedBy ?? Guid.NewGuid());

    [Fact]
    public void Create_ShouldReturnMedia_WithCorrectProperties()
    {
        var uploadedBy = Guid.NewGuid();
        var media = BuildMedia(uploadedBy);

        Assert.NotEqual(Guid.Empty, media.Id);
        Assert.Equal("books/2026/05/01/test.jpg", media.ObjectKey);
        Assert.Equal("books", media.BucketName);
        Assert.Equal("image/jpeg", media.MimeType);
        Assert.Equal(100_000, media.Size);
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal(uploadedBy, media.UploadedBy);
    }

    [Fact]
    public void CanDelete_ShouldReturnSuccess_WhenOwner()
    {
        var ownerId = Guid.NewGuid();
        var media   = BuildMedia(ownerId);

        var result = media.CanDelete(ownerId, isAdmin: false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CanDelete_ShouldReturnSuccess_WhenAdmin()
    {
        var media = BuildMedia();

        var result = media.CanDelete(Guid.NewGuid(), isAdmin: true);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CanDelete_ShouldReturnForbidden_WhenNotOwnerNotAdmin()
    {
        var media = BuildMedia();

        var result = media.CanDelete(Guid.NewGuid(), isAdmin: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("Media.Forbidden", result.Error.Code);
    }
}
