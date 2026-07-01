using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;

namespace WeddingPhotos.Api.Services;

public static class AdminAuth
{
    public static bool IsAuthorized(HttpRequestData req)
    {
        var expectedUser = Environment.GetEnvironmentVariable("AdminUsername");
        var expectedPass = Environment.GetEnvironmentVariable("AdminPassword");
        if (string.IsNullOrEmpty(expectedUser) || string.IsNullOrEmpty(expectedPass))
        {
            return false;
        }

        if (!req.Headers.TryGetValues("Authorization", out var values))
        {
            return false;
        }

        var header = values.FirstOrDefault();
        if (header is null || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..]));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex < 0)
            {
                return false;
            }

            var user = decoded[..separatorIndex];
            var pass = decoded[(separatorIndex + 1)..];

            return FixedTimeEquals(user, expectedUser) && FixedTimeEquals(pass, expectedPass);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        if (bytesA.Length != bytesB.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
