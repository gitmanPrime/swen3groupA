using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Services;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DMS.UnitTests.Services;

public class CollectionServiceTests
{
    private readonly Mock<ICollectionRepository> _collections = new();

    private CollectionService CreateSut() => new(_collections.Object, NullLogger<CollectionService>.Instance);

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidName_CreatesCollection()
    {
        var result = await CreateSut().CreateAsync("  Invoices  ", "All invoices");

        Assert.Equal("Invoices", result.Name);
        Assert.Equal("All invoices", result.Description);
        Assert.Equal(0, result.DocumentCount);
        Assert.NotEqual(Guid.Empty, result.Id);
        _collections.Verify(c => c.AddAsync(It.Is<Collection>(x => x.Name == "Invoices"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_MissingName_ThrowsValidationException(string? name)
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateSut().CreateAsync(name!));
    }

    [Fact]
    public async Task Create_NameTooLong_ThrowsValidationException()
    {
        var longName = new string('a', 129);

        await Assert.ThrowsAsync<ValidationException>(() => CreateSut().CreateAsync(longName));
    }

    // ---------- List ----------

    [Fact]
    public async Task List_ReturnsDtosWithDocumentCounts()
    {
        var first = new Collection("A");
        var second = new Collection("B");
        _collections.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Collection> { first, second });
        _collections.Setup(c => c.CountDocumentsAsync(first.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _collections.Setup(c => c.CountDocumentsAsync(second.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateSut().ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].DocumentCount);
        Assert.Equal(0, result[1].DocumentCount);
    }

    // ---------- GetById ----------

    [Fact]
    public async Task GetById_ExistingCollection_ReturnsDto()
    {
        var collection = new Collection("Invoices");
        _collections.Setup(c => c.GetByIdAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collections.Setup(c => c.CountDocumentsAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await CreateSut().GetByIdAsync(collection.Id);

        Assert.Equal(collection.Id, result.Id);
        Assert.Equal("Invoices", result.Name);
        Assert.Equal(3, result.DocumentCount);
    }

    [Fact]
    public async Task GetById_MissingCollection_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _collections.Setup(c => c.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Collection?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().GetByIdAsync(id));
    }

    // ---------- Rename ----------

    [Fact]
    public async Task Rename_ValidName_UpdatesName()
    {
        var collection = new Collection("Invoices");
        _collections.Setup(c => c.GetByIdAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collections.Setup(c => c.CountDocumentsAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().RenameAsync(collection.Id, "  Receipts  ");

        Assert.Equal("Receipts", result.Name);
        _collections.Verify(c => c.Update(collection), Times.Once);
        _collections.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rename_EmptyName_ThrowsValidationException()
    {
        var collection = new Collection("Invoices");
        _collections.Setup(c => c.GetByIdAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);

        await Assert.ThrowsAsync<ValidationException>(() => CreateSut().RenameAsync(collection.Id, " "));
    }

    [Fact]
    public async Task Rename_MissingCollection_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _collections.Setup(c => c.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Collection?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().RenameAsync(id, "New name"));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_EmptyCollection_RemovesCollection()
    {
        var collection = new Collection("Empty");
        _collections.Setup(c => c.GetByIdAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collections.Setup(c => c.CountDocumentsAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await CreateSut().DeleteAsync(collection.Id);

        _collections.Verify(c => c.Remove(collection), Times.Once);
        _collections.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_NonEmptyCollection_ThrowsConflictException()
    {
        var collection = new Collection("Full");
        _collections.Setup(c => c.GetByIdAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collections.Setup(c => c.CountDocumentsAsync(collection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateSut().DeleteAsync(collection.Id));

        Assert.Contains("cannot be deleted", ex.Message);
        _collections.Verify(c => c.Remove(It.IsAny<Collection>()), Times.Never);
        _collections.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_MissingCollection_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _collections.Setup(c => c.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Collection?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().DeleteAsync(id));
    }
}