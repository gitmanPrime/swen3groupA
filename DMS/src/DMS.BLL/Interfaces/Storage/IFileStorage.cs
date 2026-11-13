namespace DMS.BLL.Interfaces.Storage;

// handle for a file saved in a file storage backend
public record StoredFile(string Key);

/// <summary>
/// Stores and retrieves original document files by their storage key.
/// </summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string? fileName, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
