using System.Net.Http.Json;
using DMS.BLL.Dtos;

namespace DMS.IndexingWorker;

internal sealed class ElasticsearchDocumentWriter(HttpClient client)
{
    public async Task IndexAsync(OcrResult result, CancellationToken cancellationToken)
    {
        var document = new
        {
            documentId = result.DocumentId,
            text = result.Text
        };

        using var response = await client.PutAsJsonAsync(
            $"_doc/{result.DocumentId:D}",
            document,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}