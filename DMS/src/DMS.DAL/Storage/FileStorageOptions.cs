namespace DMS.DAL.Storage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Root directory for stored files. If it's empty, a default directory under
    /// the application content root is used (App_Data/files).
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string Bucket { get; set; } = "documents";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
