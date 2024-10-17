using Awaken.Silo.MongoDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;
using Serilog;

namespace Awaken.Silo.MongoDB;

public class AwakenMongoGrainStorage : MongoGrainStorage
{
    private readonly GrainCollectionNameOptions _grainCollectionNameOptions;
    
    public AwakenMongoGrainStorage(IMongoClientFactory mongoClientFactory, ILogger<MongoGrainStorage> logger,
        MongoDBGrainStorageOptions options, IOptionsSnapshot<GrainCollectionNameOptions> grainCollectionNameOptions)
        : base(mongoClientFactory, logger, options)
    {
        _grainCollectionNameOptions = grainCollectionNameOptions.Value;
    }

    protected override string ReturnGrainName<T>(string stateName, GrainId grainId)
    {
        //todo remove
        if (_grainCollectionNameOptions.GrainSpecificCollectionName.ContainsKey(typeof(T).FullName))
        {
            Log.Information($"ReturnGrainName, FullName: {typeof(T).FullName}, grainName: {_grainCollectionNameOptions.GrainSpecificCollectionName[typeof(T).FullName]}");
        }
        else
        {
            Log.Information($"ReturnGrainName, FullName: {typeof(T).FullName}, can't find in GrainSpecificCollectionName.");
            foreach (var kv in _grainCollectionNameOptions.GrainSpecificCollectionName)
            {
                Log.Information($"ReturnGrainName, GrainSpecificCollectionName, {kv.Key}:{kv.Value}");
            }
        }
        //todo remove
        
        return _grainCollectionNameOptions.GrainSpecificCollectionName.TryGetValue(typeof(T).FullName,
            out var grainName)
            ? grainName
            : base.ReturnGrainName<T>(stateName, grainId);
    }
}