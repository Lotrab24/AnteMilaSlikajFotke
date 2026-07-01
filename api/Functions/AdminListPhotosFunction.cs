using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using WeddingPhotos.Api.Services;

namespace WeddingPhotos.Api.Functions;

public class AdminListPhotosFunction
{
    private readonly PhotoStorageService _photoService;

    public AdminListPhotosFunction(PhotoStorageService photoService)
    {
        _photoService = photoService;
    }

    [Function("AdminListPhotos")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/photos")] HttpRequestData req)
    {
        if (!AdminAuth.IsAuthorized(req))
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorized.Headers.Add("WWW-Authenticate", "Basic realm=\"admin\"");
            return unauthorized;
        }

        var photos = await _photoService.ListPhotosAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(photos);
        return response;
    }
}
