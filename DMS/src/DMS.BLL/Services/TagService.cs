using DMS.BLL.Dtos;
using DMS.BLL.Exceptions;
using DMS.BLL.Interfaces.Repositories;
using DMS.BLL.Interfaces.Services;
using DMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DMS.BLL.Services;

public class TagService : ITagService
{
    private const int MaxNameLength = 64;

    private readonly ITagRepository _tags;
    private readonly ILogger<TagService> _logger;

    public TagService(ITagRepository tags, ILogger<TagService> logger)
    {
        _tags = tags;
        _logger = logger;
    }

    // create a tag
    public async Task<TagDto> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NameGuard.RequireName(name, "name", MaxNameLength);
        if (await _tags.GetByNameAsync(normalizedName, cancellationToken) is not null)
        {
            throw new ConflictException($"A tag with the name '{normalizedName}' already exists.");
        }

        var tag = new Tag(normalizedName);
        await _tags.AddAsync(tag, cancellationToken);
        await _tags.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tag {TagId} ('{Name}')", tag.Id, tag.Name);
        return ToDto(tag);
    }

    // list all tags
    public async Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tags.GetAllAsync(cancellationToken);
        return tags.Select(ToDto).ToList();
    }

    private static TagDto ToDto(Tag tag) => new(tag.Id, tag.Name);
}