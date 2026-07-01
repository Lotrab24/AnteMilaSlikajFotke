using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using WeddingPhotos.Api.Models;
using WeddingPhotos.Api.Services;

namespace WeddingPhotos.Api.Functions;

public class RequestUploadFunction
{
    private readonly PhotoStorageService _photoService;

    public RequestUploadFunction(PhotoStorageService photoService)
    {
        _photoService = photoService;
    }

    [Function("RequestUpload")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "photos/request-upload")] HttpRequestData req)
    {
        var dto = await req.ReadFromJsonAsync<RequestUploadDto>();
        if (dto is null || string.IsNullOrWhiteSpace(dto.GuestId) || string.IsNullOrWhiteSpace(dto.FileName))
        {
            return await WriteJson(req, HttpStatusCode.BadRequest,
                new RequestUploadResponse(false, null, null, 0, "Nedostaju podaci (guestId ili fileName)."));
        }

        var (allowed, usedSoFar) = await _photoService.CheckSlotAsync(dto.GuestId);
        if (!allowed)
        {
            return await WriteJson(req, HttpStatusCode.Forbidden,
                new RequestUploadResponse(false, null, null, usedSoFar, "Dosegnut je limit od 10 fotografija po gostu."));
        }

        var extension = Path.GetExtension(dto.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var blobName = $"{Sanitize(dto.GuestId)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        var uploadUrl = _photoService.GenerateUploadSasUri(blobName);

        return await WriteJson(req, HttpStatusCode.OK,
            new RequestUploadResponse(true, uploadUrl.ToString(), blobName, usedSoFar, null));
    }

    private static string Sanitize(string input)
    {
        var chars = input.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        return chars.Length > 0 ? new string(chars) : "gost";
    }

    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T body)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }
}
