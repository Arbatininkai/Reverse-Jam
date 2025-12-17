using Microsoft.AspNetCore.Http;

namespace Services.CloudStorage
{
    public interface ICloudStorageService
    {
        Task<string> UploadFileAsync(string filePath, string bucketName, string fileName);
        Task<string> UploadFileAsync(IFormFile file, string bucketName, string fileName);
        Task<bool> DeleteFileAsync(string bucketName, string fileName);
        string GetFileUrl(string bucketName, string fileName);
    }
}