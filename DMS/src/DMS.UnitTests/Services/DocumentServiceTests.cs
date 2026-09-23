using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Storage;
using DMS.BLL.Services;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DMS.UnitTests.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _documents = new();
    private readonly Mock<ICollectionRepository> _collections = new();
    private readonly Mock<ITagRepository> _tags = new();
    private readonly Mock<IFileStorage> _storage = new();

    private DocumentService CreateSut() => new(
        _documents.Object,
        _collections.Object,
        _tags.Object,
        _storage.Object,
        NullLogger<DocumentService>.Instance);

    private static MemoryStream Content(string text = "hello") => new(System.Text.Encoding.UTF8.GetBytes(text));

    private static Document NewDocument(string fileName = "report.pdf", Guid? tagId = null, Guid? collectionId = null)
    {
        var document = new Document(fileName, "application/pdf", 512, "storage-key-1", "A report");
        if (tagId.HasValue)
        {
            document.DocumentTags.Add(new DocumentTag(document.Id, tagId.Value));
        }

        if (collectionId.HasValue)
        {
            document.DocumentCollections.Add(new DocumentCollection(document.Id, collectionId.Value));
        }

        return document;
    }

    // ---------- Upload ----------

    [Fact]
    public async Task Upload_ValidInput_SavesFileAndAddsDocument()
    {
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile("generated-key"));

        var service = CreateSut();
        using var stream = Content();

        var result = await service.UploadAsync("report.pdf", "application/pdf", 512, stream, "Quarterly report");

        Assert.Equal("report.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(512, result.FileSize);
        Assert.Equal("Quarterly report", result.Description);
        Assert.NotEqual(Guid.Empty, result.Id);

        _storage.Verify(s => s.SaveAsync(stream, "report.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _documents.Verify(d => d.AddAsync(It.Is<Document>(doc => doc.StorageKey == "generated-key"), It.IsAny<CancellationToken>()), Times.Once);
        _documents.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_EmptyStream_ThrowsValidationException()
    {
        var service = CreateSut();
        using var stream = Content(string.Empty);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UploadAsync("empty.txt", "text/plain", 0, stream, null));

        Assert.Contains("empty file", ex.Errors["content"].Single(), StringComparison.OrdinalIgnoreCase);
        _storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Upload_MissingFileName_ThrowsValidationException(string? fileName)
    {
        var service = CreateSut();
        using var stream = Content();

        await Assert.ThrowsAsync<ValidationException>(() => service.UploadAsync(fileName!, "text/plain", 10, stream, null));
    }

    // ---------- GetById ----------

    [Fact]
    public async Task GetById_ExistingDocument_ReturnsDtoWithTagsAndCollections()
    {
        var tagId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var document = NewDocument(tagId: tagId, collectionId: collectionId);
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

        var result = await CreateSut().GetByIdAsync(document.Id);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal([tagId], result.TagIds);
        Assert.Equal([collectionId], result.CollectionIds);
        Assert.Equal("A report", result.Description);
    }

    // ---------- List ----------

    [Fact]
    public async Task List_NoFilters_ReturnsAllDocuments()
    {
        var documents = new List<Document> { NewDocument(), NewDocument("second.pdf") };
        _documents.Setup(d => d.GetAllAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(documents);

        var result = await CreateSut().ListAsync();

        Assert.Equal(2, result.Count);
        _documents.Verify(d => d.GetAllAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_Description_UpdatesDescription()
    {
        var document = NewDocument();
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

        var result = await CreateSut().UpdateAsync(document.Id, new UpdateDocumentRequest { Description = "New description" });

        Assert.Equal("New description", result.Description);
        _documents.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Update Collections ----------
    [Fact]
    public async Task Update_CollectionAssignment_ReplacesMemberships()
    {
        var document = NewDocument();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _collections.Setup(c => c.GetByIdAsync(first, It.IsAny<CancellationToken>())).ReturnsAsync(new Collection("A"));
        _collections.Setup(c => c.GetByIdAsync(second, It.IsAny<CancellationToken>())).ReturnsAsync(new Collection("B"));

        var result = await CreateSut().UpdateAsync(document.Id, new UpdateDocumentRequest { CollectionIds = [first, second] });

        Assert.Equal(2, result.CollectionIds.Count);
        Assert.Contains(first, result.CollectionIds);
        Assert.Contains(second, result.CollectionIds);
    }

    // ---------- Download ----------

    [Fact]
    public async Task Download_ExistingFile_ReturnsContentAndMetadata()
    {
        var document = NewDocument();
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        var fileStream = Content();
        _storage.Setup(s => s.OpenReadAsync(document.StorageKey, It.IsAny<CancellationToken>())).ReturnsAsync(fileStream);

        var result = await CreateSut().DownloadAsync(document.Id);

        Assert.Same(fileStream, result.Content);
        Assert.Equal("report.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_ExistingDocument_DeletesFileAndMetadata()
    {
        var document = NewDocument();
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);

        await CreateSut().DeleteAsync(document.Id);

        _storage.Verify(s => s.DeleteAsync(document.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
        _documents.Verify(d => d.Remove(document), Times.Once);
        _documents.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- AssignTag ----------

    [Fact]
    public async Task AssignTag_NotYetAssigned_AddsMembership()
    {
        var document = NewDocument();
        var tag = new Tag("important");
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _tags.Setup(t => t.GetByIdAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var result = await CreateSut().AssignTagAsync(document.Id, tag.Id);

        Assert.Contains(tag.Id, result.TagIds);
        _documents.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignTag_AlreadyAssigned_ThrowsConflictException()
    {
        var tagId = Guid.NewGuid();
        var document = NewDocument(tagId: tagId);
        var tag = new Tag("important");
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _tags.Setup(t => t.GetByIdAsync(tagId, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        await Assert.ThrowsAsync<ConflictException>(() => CreateSut().AssignTagAsync(document.Id, tagId));
    }

    // ---------- RemoveTag ----------

    [Fact]
    public async Task RemoveTag_Assigned_RemovesMembership()
    {
        var tagId = Guid.NewGuid();
        var document = NewDocument(tagId: tagId);
        var tag = new Tag("important");
        _documents.Setup(d => d.GetByIdAsync(document.Id, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _tags.Setup(t => t.GetByIdAsync(tagId, It.IsAny<CancellationToken>())).ReturnsAsync(tag);

        var result = await CreateSut().RemoveTagAsync(document.Id, tagId);

        Assert.Empty(result.TagIds);
        _documents.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}