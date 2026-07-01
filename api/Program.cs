using System.Text.Json;
using Azure.Core.Serialization;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeddingPhotos.Api.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage nije postavljen.");

        services.AddSingleton(new BlobServiceClient(storageConnectionString));
        services.AddSingleton(new TableServiceClient(storageConnectionString));
        services.AddSingleton<PhotoStorageService>();

        services.Configure<WorkerOptions>(workerOptions =>
        {
            workerOptions.Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        });
    })
    .Build();

host.Run();
