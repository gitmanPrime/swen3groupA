namespace DMS.BLL.Interfaces.Storage;

// handle for a file saved in a file storage backend
public record StoredFile(string Key);

/// <summary>
/// Implemented with the local filesystem for Sprint 1;
/// can be replaced by MinIO in a later sprint without touching business logic :)
/// </summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string? fileName, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}