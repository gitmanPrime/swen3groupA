using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Services;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DMS.UnitTests.Services;

public class TagServiceTests
{
    private readonly Mock<ITagRepository> _tags = new();

    private TagService CreateSut() => new(_tags.Object, NullLogger<TagService>.Instance);

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidName_CreatesTag()
    {
        _tags.Setup(t => t.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        var result = await CreateSut().CreateAsync("  important  ");

        Assert.Equal("important", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
        _tags.Verify(t => t.AddAsync(It.Is<Tag>(x => x.Name == "important"), It.IsAny<CancellationToken>()), Times.Once);
        _tags.Verify(t => t.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflictException()
    {
        _tags.Setup(t => t.GetByNameAsync("important", It.IsAny<CancellationToken>())).ReturnsAsync(new Tag("important"));

        await Assert.ThrowsAsync<ConflictException>(() => CreateSut().CreateAsync("important"));
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
        var longName = new string('a', 65);

        await Assert.ThrowsAsync<ValidationException>(() => CreateSut().CreateAsync(longName));
    }

    // ---------- List ----------

    [Fact]
    public async Task List_ReturnsAllTags()
    {
        _tags.Setup(t => t.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Tag> { new("alpha"), new("beta") });

        var result = await CreateSut().ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(["alpha", "beta"], result.Select(t => t.Name).ToArray());
    }
}