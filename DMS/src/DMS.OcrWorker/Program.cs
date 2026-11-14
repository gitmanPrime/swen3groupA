using DMS.DAL;
using DMS.OcrWorker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<RabbitMqOptions>(context.Configuration.GetSection("RabbitMQ"));
        services.AddFileStorage(context.Configuration);
        services.AddHostedService<OcrWorker>();
    })
    .Build();

await host.RunAsync();
