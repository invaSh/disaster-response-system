using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace IncidentService.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _bucketName = configuration["AWS:S3:BucketName"] ?? "incident-attachments";
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var guid = Guid.NewGuid().ToString("N")[..8];
                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{timestamp}_{guid}{extension}";
                var key = $"media/{uniqueFileName}";

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.Private
                };

                await _s3Client.PutObjectAsync(putRequest, cancellationToken);

                var serviceUrl = _configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
                var url = $"{serviceUrl}/{_bucketName}/{key}";
                
                return url;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file to S3: {ex.Message}", ex);
            }
        }
    }
}
