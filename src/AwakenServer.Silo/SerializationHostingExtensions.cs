using AwakenServer.Silo.CustomOrleansSerializer;

namespace AwakenServer.Silo;

using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Cloning;

public static class SerializationHostingExtensions
{
    public static ISerializerBuilder AddCustomSerializer(
        this ISerializerBuilder builder)
    {
        var services = builder.Services;

        services.AddSingleton<EntityCustomOrleansSerializer>();
        services.AddSingleton<IGeneralizedCodec, EntityCustomOrleansSerializer>();
        services.AddSingleton<IGeneralizedCopier, EntityCustomOrleansSerializer>();
        services.AddSingleton<ITypeFilter, EntityCustomOrleansSerializer>();

        return builder;
    }
}