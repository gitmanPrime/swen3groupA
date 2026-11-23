using DMS.API.Messaging;
using DMS.GenAiWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services
    .AddOptions<GenAiOptions>()
    .Bind(builder.Configuration.GetSection("GenAi"));

builder.Services.AddHttpClient();

builder.Services.AddScoped<IGenAiSummarizer, GeminiSummarizer>();
builder.Services.AddHostedService<GenAiWorker>();

var host = builder.Build();
await host.RunAsync();