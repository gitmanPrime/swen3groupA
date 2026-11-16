using System.Net.Http.Json;
using System.Text.Json;
using DMS.BLL.Interfaces.Repositories;

namespace DMS.DAL.Search;

public sealed class ElasticsearchDocumentSearchIndex(HttpClient client)
    : IDocumentSearchIndex
{
    public async Task<IReadOnlyList<Guid>> SearchIdsAsync(
        string query, CancellationToken cancellationToken)
    {
        var request = new
        {
            size = 50,
            query = new { match = new { text = query } }
        };

        using var response = await client.PostAsJsonAsync(
            "_search?_source=documentId", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var result = await JsonDocument.ParseAsync(
            content, cancellationToken: cancellationToken);

        var ids = new List<Guid>();
        foreach (var hit in result.RootElement.GetProperty("hits").GetProperty("hits").EnumerateArray())
        {
            var documentId = hit.GetProperty("_source").GetProperty("documentId");
            if (documentId.TryGetGuid(out var id))
                ids.Add(id);
        }

        return ids;
    }
}