namespace DMS.DAL.Storage;

// configuration for the local file storage backend
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Root directory for stored files. If it's empty, a default directory under
    /// the application content root is used (App_Data/files).
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}