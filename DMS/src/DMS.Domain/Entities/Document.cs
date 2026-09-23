namespace DMS.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    // original file name as uploaded by the user
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    // size of the stored file in bytes
    public long FileSize { get; set; }

    // UTC timestamp of when the document was uploaded
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    public string? Description { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    /// <summary>
    /// Key under which the file is stored in the configured file storage backend.
    /// Only metadata is kept in the database.
    /// </summary>
    public string StorageKey { get; private set; } = string.Empty;

    public ICollection<DocumentTag> DocumentTags { get; } = new List<DocumentTag>();

    public ICollection<DocumentCollection> DocumentCollections { get; } = new List<DocumentCollection>();

    public Document(string fileName, string contentType, long fileSize, string storageKey, string? description = null)
    {
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        StorageKey = storageKey;
        Description = description;
    }

    // required for EF Core
    private Document()
    {
    }
}