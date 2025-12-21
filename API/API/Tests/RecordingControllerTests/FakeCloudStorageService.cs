using Services.CloudStorage;

public class FakeCloudStorageService : ICloudStorageService
{
    public Task<string> UploadFileAsync(string filePath, string bucketName, string fileName)
        => Task.FromResult($"https://fake-s3/{bucketName}/{fileName}");

    public Task<string> UploadFileAsync(IFormFile file, string bucketName, string fileName)
        => Task.FromResult($"https://fake-s3/{bucketName}/{fileName}");

    public Task<bool> DeleteFileAsync(string bucketName, string fileName)
        => Task.FromResult(true);

    public string GetFileUrl(string bucketName, string fileName)
        => $"https://fake-s3/{bucketName}/{fileName}";

    public string GetPreSignedUrl(string bucketName, string fileName, TimeSpan expiration)
        => $"https://fake-s3/{bucketName}/{fileName}?expires=123";
}
