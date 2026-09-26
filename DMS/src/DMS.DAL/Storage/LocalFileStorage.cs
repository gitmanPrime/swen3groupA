using DMS.BLL.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace DMS.DAL.Storage;

/// <summary>
/// Stores document files on the local filesystem.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        var configured = options.Value.RootPath;
        _rootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "files")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string? fileName, CancellationToken cancellationToken = default)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var extension = string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{extension}";

        await using var fileStream = File.Create(GetFullPath(key));
        await content.CopyToAsync(fileStream, cancellationToken);

        return new StoredFile(key);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(key);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        File.Delete(GetFullPath(key));
        return Task.CompletedTask;
    }

    // resolves a storage key to an absolute path to guard against path traversal
    private string GetFullPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A storage key is required.", nameof(key));

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key));

        if (!fullPath.StartsWith(_rootPath  , StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Storage key '{key}' is not valid.", nameof(key));

        return fullPath;
    }
}