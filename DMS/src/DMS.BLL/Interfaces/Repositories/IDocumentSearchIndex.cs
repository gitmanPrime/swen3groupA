namespace DMS.BLL.Interfaces.Repositories;

public interface IDocumentSearchIndex
{
    Task<IReadOnlyList<Guid>> SearchIdsAsync(
        string query, CancellationToken cancellationToken);
}