using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Services;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DMS.BLL.Services;

public class CollectionService : ICollectionService
{
    private const int MaxNameLength = 128;

    private readonly ICollectionRepository _collections;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(ICollectionRepository collections, ILogger<CollectionService> logger)
    {
        _collections = collections;
        _logger = logger;
    }

    // create a collection
    public async Task<CollectionDto> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = NameGuard.RequireName(name, "name", MaxNameLength);

        var collection = new Collection(normalizedName, NormalizeDescription(description));
        await _collections.AddAsync(collection, cancellationToken);
        await _collections.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created collection {CollectionId} ('{Name}')", collection.Id, collection.Name);
        return ToDto(collection, 0);
    }

    // list collections
    public async Task<IReadOnlyList<CollectionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var collections = await _collections.GetAllAsync(cancellationToken);
        var result = new List<CollectionDto>(collections.Count);
        foreach (var collection in collections)
        {
            var count = await _collections.CountDocumentsAsync(collection.Id, cancellationToken);
            result.Add(ToDto(collection, count));
        }

        return result;
    }

    // get collection details (by id)
    public async Task<CollectionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = await GetExistingCollectionAsync(id, cancellationToken);
        var count = await _collections.CountDocumentsAsync(collection.Id, cancellationToken);
        return ToDto(collection, count);
    }

    // rename a collection (by id)
    public async Task<CollectionDto> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NameGuard.RequireName(name, "name", MaxNameLength);
        var collection = await GetExistingCollectionAsync(id, cancellationToken);

        collection.Name = normalizedName;
        _collections.Update(collection);
        await _collections.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Renamed collection {CollectionId} to '{Name}'", collection.Id, collection.Name);
        var count = await _collections.CountDocumentsAsync(collection.Id, cancellationToken);
        return ToDto(collection, count);
    }

    // delete a collection (by id)
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = await GetExistingCollectionAsync(id, cancellationToken);

        var documentCount = await _collections.CountDocumentsAsync(collection.Id, cancellationToken);
        if (documentCount > 0)
        {
            throw new ConflictException(
                $"Collection '{collection.Name}' contains {documentCount} document(s) and cannot be deleted. " +
                "Remove the documents from the collection first.");
        }

        _collections.Remove(collection);
        await _collections.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted collection {CollectionId}", collection.Id);
    }


    private async Task<Collection> GetExistingCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _collections.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Collection", id);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
            return null;

        var trimmed = description.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static CollectionDto ToDto(Collection collection, int documentCount) => new(collection.Id, collection.Name, collection.Description, collection.CreatedAt, documentCount);
}