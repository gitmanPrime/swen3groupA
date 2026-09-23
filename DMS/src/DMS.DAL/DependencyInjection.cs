using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Storage;
using DMS.DAL.Repositories;
using DMS.DAL.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        return services;
    }
}