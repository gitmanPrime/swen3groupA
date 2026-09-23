using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Services;
using DMS.BLL.Interfaces.Storage;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DMS.BLL.Services;

public class DocumentService : IDocumentService
{
    private const int MaxFileNameLength = 255;

    private readonly IDocumentRepository _documents;
    private readonly ICollectionRepository _collections;
    private readonly ITagRepository _tags;
    private readonly IFileStorage _storage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IDocumentRepository documents, ICollectionRepository collections, ITagRepository tags, IFileStorage storage, ILogger<DocumentService> logger)
    {
        _documents = documents;
        _collections = collections;
        _tags = tags;
        _storage = storage;
        _logger = logger;
    }

    // upload a document
    public async Task<DocumentDto> UploadAsync(string fileName, string contentType, long fileSize, Stream content, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ValidationException("fileName", "A file name is required.");

        if (fileName.Length > MaxFileNameLength)
            throw new ValidationException("fileName", $"File name must not exceed {MaxFileNameLength} characters.");

        if (content is null)
            throw new ValidationException("content", "File content is required.");

        if (content.Length == 0)
            throw new ValidationException("content", "An empty file cannot be uploaded.");

        if (fileSize <= 0)
            throw new ValidationException("fileSize", "File size must be greater than zero.");

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

        // Persist the file first so a storage failure never leaves orphaned metadata.
        var storedFile = await _storage.SaveAsync(content, fileName, cancellationToken);

        var document = new Document(fileName.Trim(), normalizedContentType, fileSize, storedFile.Key, NormalizeDescription(description));

        await _documents.AddAsync(document, cancellationToken);
        await _documents.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Uploaded document {DocumentId} ({FileName}, {FileSize} bytes, {ContentType})",
            document.Id, document.FileName, document.FileSize, document.ContentType);

        return ToDto(document);
    }

    // list documents (with optional collectionid and tagid filters)
    public async Task<IReadOnlyList<DocumentDto>> ListAsync(Guid? collectionId = null, Guid? tagId = null, CancellationToken cancellationToken = default)
    {
        if (collectionId.HasValue && await _collections.GetByIdAsync(collectionId.Value, cancellationToken) is null)
            throw new NotFoundException("Collection", collectionId.Value);

        if (tagId.HasValue && await _tags.GetByIdAsync(tagId.Value, cancellationToken) is null)
            throw new NotFoundException("Tag", tagId.Value);

        var documents = await _documents.GetAllAsync(collectionId, tagId, cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    // get document metadata (by id)
    public async Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);
        return ToDto(document);
    }

    // download the original file (by id)
    public async Task<FileDownload> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);

        var content = await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
        if (content is null)
        {
            _logger.LogWarning(
                "Stored file for document {DocumentId} (key {StorageKey}) is missing",
                document.Id, document.StorageKey);
            throw new NotFoundException($"The stored file for document '{id}' is missing.");
        }

        return new FileDownload(content, document.FileName, document.ContentType);
    }

    // update document data (by id)
    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ValidationException("Request body is required.");

        var document = await GetExistingDocumentAsync(id, cancellationToken);

        if (request.Description is not null)
            document.Description = NormalizeDescription(request.Description);

        if (request.CollectionIds is not null)
            await ReplaceCollectionsAsync(document, request.CollectionIds, cancellationToken);

        _documents.Update(document);
        await _documents.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated document {DocumentId}", document.Id);
        return ToDto(document);
    }

    // delete a document (by id)
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(id, cancellationToken);

        await _storage.DeleteAsync(document.StorageKey, cancellationToken);
        _documents.Remove(document);
        await _documents.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted document {DocumentId}", document.Id);
    }

    // assign a tag
    public async Task<DocumentDto> AssignTagAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(documentId, cancellationToken);
        var tag = await GetExistingTagAsync(tagId, cancellationToken);

        if (document.DocumentTags.Any(dt => dt.TagId == tagId))
            throw new ConflictException($"Tag '{tag.Name}' is already assigned to document '{documentId}'.");

        document.DocumentTags.Add(new DocumentTag(documentId, tagId));
        await _documents.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned tag {TagId} to document {DocumentId}", tagId, documentId);
        return ToDto(document);
    }

    // remove a tag
    public async Task<DocumentDto> RemoveTagAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var document = await GetExistingDocumentAsync(documentId, cancellationToken);
        await GetExistingTagAsync(tagId, cancellationToken);

        var membership = document.DocumentTags.FirstOrDefault(dt => dt.TagId == tagId);
        if (membership is null)
            throw new NotFoundException($"Tag '{tagId}' is not assigned to document '{documentId}'.");

        document.DocumentTags.Remove(membership);
        await _documents.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed tag {TagId} from document {DocumentId}", tagId, documentId);
        return ToDto(document);
    }


    private async Task ReplaceCollectionsAsync(Document document, IReadOnlyList<Guid> collectionIds, CancellationToken cancellationToken)
    {
        foreach (var collectionId in collectionIds.Distinct())
        {
            if (await _collections.GetByIdAsync(collectionId, cancellationToken) is null)
                throw new NotFoundException("Collection", collectionId);
        }

        document.DocumentCollections.Clear();
        foreach (var collectionId in collectionIds.Distinct())
        {
            document.DocumentCollections.Add(new DocumentCollection(document.Id, collectionId));
        }
    }

    private async Task<Document> GetExistingDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _documents.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Document", id);
    }

    private async Task<Tag> GetExistingTagAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _tags.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Tag", id);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
            return null;

        var trimmed = description.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static DocumentDto ToDto(Document document) => new(
        document.Id,
        document.FileName,
        document.ContentType,
        document.FileSize,
        document.UploadedAt,
        document.Description,
        document.Status,
        document.DocumentCollections.Select(dc => dc.CollectionId).ToList(),
        document.DocumentTags.Select(dt => dt.TagId).ToList());
}