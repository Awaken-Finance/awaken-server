using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using AwakenServer.Tokens;
using Serilog;

namespace Awaken.Silo;

public class AwakenJsonGrainStateSerializer: IGrainStateSerializer
{
    private readonly JsonSerializerSettings jsonSettings;

    public AwakenJsonGrainStateSerializer(IOptions<JsonGrainStateSerializerOptions> options, IServiceProvider serviceProvider)
    {
        jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);
        jsonSettings.Converters.Add(new TradePairWithTokenConverter());
        jsonSettings.Converters.Add(new TokenConverter());
        options.Value.ConfigureJsonSerializerSettings(jsonSettings);
    }

    public T Deserialize<T>(BsonValue value)
    {
        using var jsonReader = new JTokenReader(value.ToJToken());
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return localSerializer.Deserialize<T>(jsonReader);
    }

    public BsonValue Serialize<T>(T state)
    {
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return JObject.FromObject(state, localSerializer).ToBson();
    }
}

public class TradePairWithTokenConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TradePairWithToken) || objectType == typeof(TradePairWithTokenDto);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Log.Information($"TradePairWithTokenConverter, objectType: {objectType}");
        // Deserialize JSON to TradePairWithToken
        var tradePair = serializer.Deserialize<TradePairWithToken>(reader);
        
        // Convert TradePairWithToken to TradePairWithTokenDto
        if (tradePair != null)
        {
            Log.Information($"TokenConverter, objectType: {objectType}, return TradePairWithTokenDto");
            return new TradePairWithTokenDto
            {
                Id = tradePair.Id,
                ChainId = tradePair.ChainId,
                Address = tradePair.Address,
                FeeRate = tradePair.FeeRate,
                IsTokenReversed = tradePair.IsTokenReversed,
                Token0 = new TokenDto()
                {
                    Address = tradePair.Token0.Address,
                    ChainId = tradePair.Token0.ChainId,
                    Decimals = tradePair.Token0.Decimals,
                    Id = tradePair.Token0.Id,
                    ImageUri = tradePair.Token0.ImageUri,
                    Symbol = tradePair.Token0.Symbol
                },
                Token1 = new TokenDto()
                {
                    Address = tradePair.Token1.Address,
                    ChainId = tradePair.Token1.ChainId,
                    Decimals = tradePair.Token1.Decimals,
                    Id = tradePair.Token1.Id,
                    ImageUri = tradePair.Token1.ImageUri,
                    Symbol = tradePair.Token1.Symbol
                }
            };
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is TradePairWithTokenDto dto)
        {
            // Convert TradePairWithTokenDto to TradePairWithToken
            var tradePair = new TradePairWithToken
            {
                Id = dto.Id,
                ChainId = dto.ChainId,
                Address = dto.Address,
                FeeRate = dto.FeeRate,
                IsTokenReversed = dto.IsTokenReversed,
                Token0 = new Token
                {
                    Address = dto.Token0.Address,
                    ChainId = dto.Token0.ChainId,
                    Decimals = dto.Token0.Decimals,
                    Id = dto.Token0.Id,
                    ImageUri = dto.Token0.ImageUri,
                    Symbol = dto.Token0.Symbol
                },
                Token1 = new Token
                {
                    Address = dto.Token1.Address,
                    ChainId = dto.Token1.ChainId,
                    Decimals = dto.Token1.Decimals,
                    Id = dto.Token1.Id,
                    ImageUri = dto.Token1.ImageUri,
                    Symbol = dto.Token1.Symbol
                }
            };

            // Serialize using default serialization
            serializer.Serialize(writer, tradePair);
        }
    }
}


public class TokenConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Token) || objectType == typeof(TokenDto);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Log.Information($"TokenConverter, objectType: {objectType}");
        // Deserialize JSON to Token
        var token = serializer.Deserialize<Token>(reader);

        // Convert Token to TokenDto
        if (token != null)
        {
            Log.Information($"TokenConverter, objectType: {objectType}, return TokenDto");
            return new TokenDto
            {
                Id = token.Id,
                Address = token.Address,
                Symbol = token.Symbol,
                Decimals = token.Decimals,
                ImageUri = token.ImageUri,
                ChainId = token.ChainId
            };
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is TokenDto tokenDto)
        {
            // Convert TokenDto to Token
            var token = new Token
            {
                Id = tokenDto.Id,
                Address = tokenDto.Address,
                Symbol = tokenDto.Symbol,
                Decimals = tokenDto.Decimals,
                ImageUri = tokenDto.ImageUri,
                ChainId = tokenDto.ChainId
            };

            // Serialize using default serialization
            serializer.Serialize(writer, token);
        }
    }
}