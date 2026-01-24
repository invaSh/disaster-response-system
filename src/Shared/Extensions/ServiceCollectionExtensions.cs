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
        var serviceUrl = configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
        var region = configuration["AWS:Region"] ?? "us-east-1";

        var credentials = new BasicAWSCredentials("test", "test");

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = region
            };
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var config = new AmazonSQSConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            };
            return new AmazonSQSClient(credentials, config);
        });

        services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
        {
            var config = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            };
            return new AmazonSimpleNotificationServiceClient(credentials, config);
        });

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