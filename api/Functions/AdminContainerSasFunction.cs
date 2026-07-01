using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using WeddingPhotos.Api.Services;

namespace WeddingPhotos.Api.Functions;

public class AdminContainerSasFunction
{
    private readonly PhotoStorageService _photoService;

    public AdminContainerSasFunction(PhotoStorageService photoService)
    {
        _photoService = photoService;
    }

    [Function("AdminContainerSas")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gallery/download-info")] HttpRequestData req)
    {
        if (!AdminAuth.IsAuthorized(req))
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            unauthorized.Headers.Add("WWW-Authenticate", "Basic realm=\"admin\"");
            return unauthorized;
        }

        var sas = _photoService.GenerateContainerSas();
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(sas);
        return response;
    }
}
