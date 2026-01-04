using BookStore.Domain.Entities.Identity;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Identity;
using Moq;

namespace BookStore.Application.Tests.Common;

public abstract class ServiceTestBase
{
    protected readonly Mock<IUnitOfWork> UowMock;
    protected readonly Mock<IUserDeviceRepository> UserDeviceRepoMock;

    protected ServiceTestBase()
    {
        UserDeviceRepoMock
        .Setup(r => r.GetListAsync(
        It.IsAny<System.Linq.Expressions.Expression<Func<UserDevice, bool>>>(),
        It.IsAny<Func<IQueryable<UserDevice>, IOrderedQueryable<UserDevice>>>(),
        It.IsAny<int?>(),
        It.IsAny<int?>()
        ))
        .ReturnsAsync(new List<UserDevice>());

        UowMock = new Mock<IUnitOfWork>();
        UserDeviceRepoMock = new Mock<IUserDeviceRepository>();

        UowMock
            .Setup(u => u.UserDevices)
            .Returns(UserDeviceRepoMock.Object);
    }
}
