using DMS.BLL.Dtos;

namespace DMS.BLL.Interfaces.Services;

// result of a download request: the file content and the metadata needed to serve it
public record FileDownload(Stream Content, string FileName, string ContentType);

public interface IDocumentService
{
    // upload a document
    Task<DocumentDto> UploadAsync(string fileName, string contentType, long fileSize, Stream content, string? description, CancellationToken cancellationToken = default);
    // list documents (with optional collectionid and tagid filters)
    Task<IReadOnlyList<DocumentDto>> ListAsync(Guid? collectionId = null, Guid? tagId = null, CancellationToken cancellationToken = default);
    // get document metadata (by id)
    Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // download the original file (by id)
    Task<FileDownload> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
    // update document data (by id)
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);
    // delete a document (by id)
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    // assign a tag
    Task<DocumentDto> AssignTagAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);
    // remove a tag
    Task<DocumentDto> RemoveTagAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);
}