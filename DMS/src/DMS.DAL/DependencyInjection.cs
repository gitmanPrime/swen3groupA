using Amazon.Runtime;
using Amazon.S3;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Storage;
using DMS.DAL.Repositories;
using DMS.DAL.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMS.DAL;

// registers the data access layer: EF Core DbContext, repositories and file storage
public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The PostgreSQL connection string 'ConnectionStrings:Postgres' is not configured.");
        }

        services.AddDbContext<DmsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"), "FileStorage:Endpoint must be an HTTP(S) URL.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.AccessKey) && !string.IsNullOrWhiteSpace(o.SecretKey),
                "MinIO credentials must be configured in FileStorage:AccessKey and FileStorage:SecretKey.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Bucket), "FileStorage:Bucket is required.")
            .ValidateOnStart();
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var storage = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            return new AmazonS3Client(storage.AccessKey, storage.SecretKey, new AmazonS3Config
            {
                ServiceURL = storage.Endpoint,
                AuthenticationRegion = "us-east-1",
                ForcePathStyle = true,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                Timeout = TimeSpan.FromSeconds(10),
                MaxErrorRetry = 1
            });
        });
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<IFileStorage, MinioFileStorage>();

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        return services;
    }
}
