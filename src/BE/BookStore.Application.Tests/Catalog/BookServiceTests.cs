using BookStore.Application.Services.Catalog;
using BookStore.Application.Dtos.CatalogDto.Book;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Domain.IRepository.Catalog;
using BookStore.Application.IService.Storage;
using BookStore.Shared.Common;
using FluentAssertions;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;

public class BookServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBookRepository> _books = new();
    private readonly Mock<IPublisherRepository> _publishers = new();
    private readonly Mock<IBookAuthorRepository> _bookAuthors = new();
    private readonly Mock<IBookCategoryRepository> _bookCategories = new();
    private readonly Mock<IStorageService> _storage = new();

    private readonly BookService _service;

    public BookServiceTests()
    {
        _uow.Setup(x => x.Books).Returns(_books.Object);
        _uow.Setup(x => x.Publishers).Returns(_publishers.Object);
        _uow.Setup(x => x.BookAuthor).Returns(_bookAuthors.Object);
        _uow.Setup(x => x.BookCategory).Returns(_bookCategories.Object);

        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new BookService(_uow.Object, _storage.Object);
    }

    // ================= HELPER =================
    private static Book CreateFullBook(Guid id)
    {
        return new Book
        {
            Id = id,
            Title = "Clean Code",
            ISBN = "123",
            Publisher = new Publisher { Name = "NXB Trẻ" },
            BookAuthors = new List<BookAuthor>
            {
                new BookAuthor
                {
                    Author = new Author { Name = "Robert C. Martin" }
                }
            },
            BookCategories = new List<BookCategory>
            {
                new BookCategory
                {
                    Category = new Category { Name = "Programming" }
                }
            }
        };
    }

    // ================= CREATE =================

    [Fact]
    public async Task CreateAsync_DuplicatedISBN_ShouldFail()
    {
        var dto = new CreateBookRequestDto { ISBN = "123" };

        _books.Setup(x => x.ExistsByISBNAsync(dto.ISBN))
              .ReturnsAsync(true);

        var result = await _service.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateAsync_PublisherNotFound_ShouldFail()
    {
        var publisherId = Guid.NewGuid();

        var dto = new CreateBookRequestDto
        {
            ISBN = "123",
            PublisherId = publisherId,
            AuthorIds = new List<Guid>(),
            CategoryIds = new List<Guid>()
        };

        _books.Setup(x => x.ExistsByISBNAsync(dto.ISBN))
              .ReturnsAsync(false);

        _publishers.Setup(x => x.GetByIdAsync(publisherId))
                   .ReturnsAsync((Publisher?)null);

        var result = await _service.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CreateAsync_Success_ShouldReturnBook()
    {
        var publisherId = Guid.NewGuid();

        var dto = new CreateBookRequestDto
        {
            Title = "  Clean Code ",
            ISBN = "123",
            PublisherId = publisherId,
            AuthorIds = new List<Guid> { Guid.NewGuid() },
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        };

        _books.Setup(x => x.ExistsByISBNAsync(dto.ISBN))
              .ReturnsAsync(false);

        _publishers.Setup(x => x.GetByIdAsync(publisherId))
                   .ReturnsAsync(new Publisher { Id = publisherId });

        _books.Setup(x => x.GetDetailAsync(It.IsAny<Guid>()))
              .ReturnsAsync(CreateFullBook(Guid.NewGuid()));

        var result = await _service.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Clean Code");
        result.Value.Authors.Should().Contain("Robert C. Martin");
    }

    // ================= GET BY ID =================

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldFail()
    {
        _books.Setup(x => x.GetDetailAsync(It.IsAny<Guid>()))
              .ReturnsAsync((Book?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_Found_ShouldReturnBook()
    {
        var id = Guid.NewGuid();

        _books.Setup(x => x.GetDetailAsync(id))
              .ReturnsAsync(CreateFullBook(id));

        var result = await _service.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Publisher.Should().Be("NXB Trẻ");
    }

    // ================= GET LIST =================

    [Fact]
    public async Task GetListAsync_InvalidPaging_ShouldFail()
    {
        var result = await _service.GetListAsync(0, 10);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetListAsync_Success_ShouldReturnPagedResult()
    {
        _books
    .Setup(x => x.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>()))
    .ReturnsAsync(1);


        _books.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAsync(new List<Book> { CreateFullBook(Guid.NewGuid()) });

        var result = await _service.GetListAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Count.Should().Be(1);
        result.Value.TotalCount.Should().Be(1);
    }

    // ================= UPDATE =================

    [Fact]
    public async Task UpdateAsync_NotFound_ShouldFail()
    {
        _books.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
              .ReturnsAsync((Book?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateBookRequestDto());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_Success_ShouldUpdate()
    {
        var id = Guid.NewGuid();
        var book = CreateFullBook(id);

        _books.Setup(x => x.GetByIdAsync(id))
              .ReturnsAsync(book);

        _books.Setup(x => x.GetDetailAsync(id))
              .ReturnsAsync(book);

        var dto = new UpdateBookRequestDto { Title = " New Title " };

        var result = await _service.UpdateAsync(id, dto);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("New Title");
    }

    // ================= DELETE =================

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldFail()
    {
        _books.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
              .ReturnsAsync((Book?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Success_ShouldDelete()
    {
        var book = new Book { Id = Guid.NewGuid() };

        _books.Setup(x => x.GetByIdAsync(book.Id))
              .ReturnsAsync(book);

        var result = await _service.DeleteAsync(book.Id);

        result.IsSuccess.Should().BeTrue();
        _books.Verify(x => x.Delete(book), Times.Once);
    }
}
