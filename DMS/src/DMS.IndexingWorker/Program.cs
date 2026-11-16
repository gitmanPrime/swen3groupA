using DMS.IndexingWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var elasticsearchUrl = builder.Configuration["Elasticsearch:Url"] ?? "http://127.0.0.1:9200";
var indexName = builder.Configuration["Elasticsearch:IndexName"] ?? "dms-documents";
var indexUrl = new Uri($"{elasticsearchUrl.TrimEnd('/')}/{Uri.EscapeDataString(indexName)}/");

builder.Services.AddSingleton(_ => new HttpClient
{
    BaseAddress = indexUrl,
    Timeout = TimeSpan.FromSeconds(30)
});
builder.Services.AddSingleton<ElasticsearchDocumentWriter>();

builder.Services.AddHostedService<IndexingWorker>();

await builder.Build().RunAsync();