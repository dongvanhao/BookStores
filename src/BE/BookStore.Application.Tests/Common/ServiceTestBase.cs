using BookStore.Domain.IRepository.Common;
using Moq;

namespace BookStore.Application.Tests.Common;

public abstract class ServiceTestBase
{
    protected readonly Mock<IDbSession> SessionMock = new();

    protected ServiceTestBase()
    {
        SessionMock.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }
}
