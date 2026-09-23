using DMS.BLL.Dtos;

namespace DMS.BLL.Interfaces.Services;

public interface ITagService
{
    // create a tag
    Task<TagDto> CreateAsync(string name, CancellationToken cancellationToken = default);
    // list tags
    Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken cancellationToken = default);
}