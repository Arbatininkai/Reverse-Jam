using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace Services.CloudStorage
{
    public class AwsS3StorageService : ICloudStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _region;

        public AwsS3StorageService(IAmazonS3 s3Client, string region = "us-east-1")
        {
            _s3Client = s3Client;
            _region = region;
        }

        public async Task<string> UploadFileAsync(string filePath, string bucketName, string fileName)
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                FilePath = filePath,
                ContentType = "audio/mpeg",
                CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(request);
            return GetFileUrl(bucketName, fileName);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string bucketName, string fileName)
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(request);
            return GetFileUrl(bucketName, fileName);
        }

        public async Task<bool> DeleteFileAsync(string bucketName, string fileName)
        {
            try
            {
                await _s3Client.DeleteObjectAsync(bucketName, fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetFileUrl(string bucketName, string fileName)
        {
            return $"https://{bucketName}.s3.{_region}.amazonaws.com/{fileName}";
        }
    }
}