using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using WeddingPhotos.Api.Models;

namespace WeddingPhotos.Api.Services;

public class PhotoStorageService
{
    private const int MaxPhotosPerGuest = 10;
    private const string ContainerName = "wedding-photos";
    private const string CounterTableName = "GuestCounters";
    private const string AuditTableName = "UploadAudit";
    private const string CounterPartitionKey = "guest";

    private readonly BlobContainerClient _containerClient;
    private readonly TableClient _counterTable;
    private readonly TableClient _auditTable;

    public PhotoStorageService(BlobServiceClient blobServiceClient, TableServiceClient tableServiceClient)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);

        _counterTable = tableServiceClient.GetTableClient(CounterTableName);
        _counterTable.CreateIfNotExists();

        _auditTable = tableServiceClient.GetTableClient(AuditTableName);
        _auditTable.CreateIfNotExists();
    }

    public async Task<(bool allowed, int usedSoFar)> CheckSlotAsync(string guestId)
    {
        var existing = await _counterTable.GetEntityIfExistsAsync<TableEntity>(CounterPartitionKey, guestId);
        var count = existing.HasValue ? existing.Value!.GetInt32("Count") ?? 0 : 0;
        return (count < MaxPhotosPerGuest, count);
    }

    public Uri GenerateUploadSasUri(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);
        return blobClient.GenerateSasUri(sasBuilder);
    }

    public async Task<bool> BlobExistsAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }

    public async Task<int> IncrementCounterAsync(string guestId)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var existing = await _counterTable.GetEntityIfExistsAsync<TableEntity>(CounterPartitionKey, guestId);
            try
            {
                if (existing.HasValue)
                {
                    var entity = existing.Value!;
                    var newCount = (entity.GetInt32("Count") ?? 0) + 1;
                    entity["Count"] = newCount;
                    await _counterTable.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
                    return newCount;
                }

                await _counterTable.AddEntityAsync(new TableEntity(CounterPartitionKey, guestId) { ["Count"] = 1 });
                return 1;
            }
            catch (RequestFailedException ex) when (ex.Status is 412 or 409)
            {
                // Netko drugi je upravo azurirao brojac za istog gosta - pokusaj ponovno.
            }
        }

        throw new InvalidOperationException($"Nije moguce azurirati brojac za gosta {guestId} nakon vise pokusaja.");
    }

    public async Task LogUploadAuditAsync(string guestId, string blobName, string? ip, string? userAgent)
    {
        var entity = new TableEntity(guestId, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}")
        {
            ["BlobName"] = blobName,
            ["Ip"] = ip ?? "unknown",
            ["UserAgent"] = userAgent ?? "unknown"
        };
        await _auditTable.AddEntityAsync(entity);
    }

    public async Task<List<PhotoInfo>> ListPhotosAsync()
    {
        var results = new List<PhotoInfo>();
        await foreach (var blobItem in _containerClient.GetBlobsAsync())
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = blobItem.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(4)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var url = blobClient.GenerateSasUri(sasBuilder);
            results.Add(new PhotoInfo(blobItem.Name, url.ToString(), blobItem.Properties.LastModified, blobItem.Properties.ContentLength));
        }

        return results.OrderByDescending(p => p.UploadedAt).ToList();
    }

    public ContainerSasResponse GenerateContainerSas()
    {
        var expiresOn = DateTimeOffset.UtcNow.AddHours(24);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            Resource = "c",
            ExpiresOn = expiresOn
        };
        sasBuilder.SetPermissions(BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List);

        var sasUri = _containerClient.GenerateSasUri(sasBuilder);
        var azCopyCommand = $"azcopy copy \"{sasUri}\" \"./vjencanje-slike\" --recursive";

        return new ContainerSasResponse(sasUri.ToString(), azCopyCommand, expiresOn);
    }
}
