namespace DMS.BLL.Exceptions;

public class FileStorageException(string message, Exception innerException) : Exception(message, innerException);
