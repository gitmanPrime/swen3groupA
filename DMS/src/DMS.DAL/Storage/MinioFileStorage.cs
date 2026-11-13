using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Storage;
using Microsoft.Extensions.Options;

namespace DMS.DAL.Storage;

public class MinioFileStorage(IAmazonS3 client, IOptions<FileStorageOptions> options, LocalFileStorage localStorage) : IFileStorage
{
    // existing database keys have no prefix and still point to the local volume
    private const string ObjectPrefix = "minio/";
    private readonly string _bucket = options.Value.Bucket;

    public async Task<StoredFile> SaveAsync(Stream content, string? fileName, CancellationToken cancellationToken = default)
    {
        var key = $"{ObjectPrefix}{Guid.NewGuid():N}";
        var startPosition = content.Position;
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            UseChunkEncoding = false
        };

        try
        {
            try
            {
                await client.PutObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
            {
                await CreateBucketAsync(cancellationToken);
                content.Position = startPosition;
                await client.PutObjectAsync(request, cancellationToken);
            }

            return new StoredFile(key);
        }
        catch (Exception ex) when (IsStorageFailure(ex, cancellationToken))
        {
            throw new FileStorageException("Could not store the document.", ex);
        }
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        // old one still works (local storage)
        if (!key.StartsWith(ObjectPrefix, StringComparison.Ordinal))
            return await localStorage.OpenReadAsync(key, cancellationToken);

        try
        {
            var response = await client.GetObjectAsync(_bucket, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
        catch (Exception ex) when (IsStorageFailure(ex, cancellationToken))
        {
            throw new FileStorageException("Could not read the document.", ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        // old one still works (local storage)
        if (!key.StartsWith(ObjectPrefix, StringComparison.Ordinal))
        {
            await localStorage.DeleteAsync(key, cancellationToken);
            return;
        }

        try
        {
            await client.DeleteObjectAsync(_bucket, key, cancellationToken);
        }
        catch (Exception ex) when (IsStorageFailure(ex, cancellationToken))
        {
            throw new FileStorageException("Could not delete the document file.", ex);
        }
    }

    private async Task CreateBucketAsync(CancellationToken cancellationToken)
    {
        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
        {
            // another upload already created the bucket
        }
    }

    private static bool IsStorageFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is AmazonServiceException or HttpRequestException or IOException or TimeoutException ||
        (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
