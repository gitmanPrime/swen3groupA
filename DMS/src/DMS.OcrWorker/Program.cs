using DMS.OcrWorker;
using RabbitMQ.Client;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<RabbitMqOptions>(context.Configuration.GetSection("RabbitMQ"));
        services.AddHostedService<OcrWorker>();
    })
    .Build();

await host.RunAsync();
