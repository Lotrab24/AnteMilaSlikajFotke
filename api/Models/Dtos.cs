namespace WeddingPhotos.Api.Models;

public record RequestUploadDto(string GuestId, string FileName);

public record RequestUploadResponse(bool Success, string? UploadUrl, string? BlobName, int UsedSoFar, string? Error);

public record ConfirmUploadDto(string GuestId, string BlobName);

public record ConfirmUploadResponse(bool Success, int UsedSoFar, string? Error);

public record PhotoInfo(string BlobName, string Url, DateTimeOffset? UploadedAt, long? SizeBytes);

public record ContainerSasResponse(string SasUrl, string AzCopyCommand, DateTimeOffset ExpiresAt);
