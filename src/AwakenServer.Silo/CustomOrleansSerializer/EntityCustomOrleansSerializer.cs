using System.Buffers;
using AwakenServer.Tokens;
using Orleans.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Newtonsoft.Json;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;
using Volo.Abp.Domain.Entities;

namespace AwakenServer.Silo.CustomOrleansSerializer;

internal sealed class EntityCustomOrleansSerializer :
    IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    void IFieldCodec.WriteField<TBufferWriter>(
        ref Writer<TBufferWriter> writer, 
        uint fieldIdDelta,
        Type expectedType,
        object value)
    {
    }

    object IFieldCodec.ReadValue<TInput>(
        ref Reader<TInput> reader, Field field)
    {
        return Activator.CreateInstance(field.FieldType);
    }
    
    public bool IsSupportedType(Type type)
    {
        return type == typeof(Entity) || type.IsSubclassOf(typeof(Entity));
    }
    
    public bool? IsTypeAllowed(Type type)
    {
        return type == typeof(Entity) || type.IsSubclassOf(typeof(Entity));
    }
    
    object IDeepCopier.DeepCopy(object input, CopyContext context)
    {
        return input;
    }
}