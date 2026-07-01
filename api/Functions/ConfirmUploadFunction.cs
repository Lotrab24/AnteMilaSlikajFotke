using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using WeddingPhotos.Api.Models;
using WeddingPhotos.Api.Services;

namespace WeddingPhotos.Api.Functions;

public class ConfirmUploadFunction
{
    private readonly PhotoStorageService _photoService;

    public ConfirmUploadFunction(PhotoStorageService photoService)
    {
        _photoService = photoService;
    }

    [Function("ConfirmUpload")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photos/confirm-upload")] HttpRequestData req)
    {
        var dto = await req.ReadFromJsonAsync<ConfirmUploadDto>();
        if (dto is null || string.IsNullOrWhiteSpace(dto.GuestId) || string.IsNullOrWhiteSpace(dto.BlobName))
        {
            return await WriteJson(req, HttpStatusCode.BadRequest,
                new ConfirmUploadResponse(false, 0, "Nedostaju podaci (guestId ili blobName)."));
        }

        var exists = await _photoService.BlobExistsAsync(dto.BlobName);
        if (!exists)
        {
            return await WriteJson(req, HttpStatusCode.BadRequest,
                new ConfirmUploadResponse(false, 0, "Upload nije pronadjen u pohrani - pokusaj ponovno."));
        }

        var usedSoFar = await _photoService.IncrementCounterAsync(dto.GuestId);

        var ip = req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor)
            ? forwardedFor.FirstOrDefault()?.Split(',')[0].Trim()
            : null;
        var userAgent = req.Headers.TryGetValues("User-Agent", out var userAgents)
            ? userAgents.FirstOrDefault()
            : null;
        await _photoService.LogUploadAuditAsync(dto.GuestId, dto.BlobName, ip, userAgent);

        return await WriteJson(req, HttpStatusCode.OK, new ConfirmUploadResponse(true, usedSoFar, null));
    }

    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T body)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }
}
