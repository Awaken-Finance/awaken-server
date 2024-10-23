using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;

namespace AwakenServer;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var configSection = context.Configuration.GetSection("Orleans");
            if (configSection == null)
                throw new ArgumentNullException(nameof(configSection), "The Orleans config node is missing");
            clientBuilder.UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .Configure<ClientMessagingOptions>(options =>
                {
                    var responseTimeoutValue = configSection.GetValue<int?>("GrainResponseTimeOut");
                    if (responseTimeoutValue.HasValue)
                    {
                        options.ResponseTimeout = TimeSpan.FromSeconds(responseTimeoutValue.Value);
                    }

                    var maxMessageBodySizeValue = configSection.GetValue<int?>("GrainMaxMessageBodySize");
                    if (maxMessageBodySizeValue.HasValue)
                    {
                        options.MaxMessageBodySize = maxMessageBodySizeValue.Value;
                    }
                })
                .AddActivityPropagation();
        });
    }
}