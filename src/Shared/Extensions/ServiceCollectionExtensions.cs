using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStackAws(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Pull the URL from appsettings (e.g., http://localhost:4566)
        // If it's missing, we'll default to localhost for easier school project setup
        var serviceUrl = configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
        var region = configuration["AWS:Region"] ?? "us-east-1";

        // 2. Dummy credentials (LocalStack doesn't care what these are, but they must exist)
        var credentials = new BasicAWSCredentials("test", "test");

        // 3. Register S3
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true, // Required for LocalStack
                AuthenticationRegion = region
            };
            return new AmazonS3Client(credentials, config);
        });

        // 4. Register SQS
        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var config = new AmazonSQSConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            };
            return new AmazonSQSClient(credentials, config);
        });

        // 5. Register SNS
        services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
        {
            var config = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            };
            return new AmazonSimpleNotificationServiceClient(credentials, config);
        });

        // 6. Register DynamoDB (Commonly used in CRUD microservices)
        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            };
            return new AmazonDynamoDBClient(credentials, config);
        });

        return services;
    }
}