using Awaken.Silo.MongoDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Awaken.Silo.MongoDB;

public class AwakenMongoGrainStorage : MongoGrainStorage
{
    private readonly GrainCollectionNameOptions _grainCollectionNameOptions;
    
    public AwakenMongoGrainStorage(IMongoClientFactory mongoClientFactory,
        ILogger<MongoGrainStorage> logger,
        IGrainStateSerializer serializer,
        MongoDBGrainStorageOptions options, 
        IOptionsSnapshot<GrainCollectionNameOptions> grainCollectionNameOptions)
        : base(mongoClientFactory, logger, serializer, options)
    {
        _grainCollectionNameOptions = grainCollectionNameOptions.Value;
    }

    protected override string ReturnGrainName<T>(string stateName, GrainId grainId)
    {
        return _grainCollectionNameOptions.GrainSpecificCollectionName.TryGetValue(typeof(T).FullName,
            out var grainName)
            ? grainName
            : base.ReturnGrainName<T>(stateName, grainId);
    }
}