using Orleans;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public interface ITokenPriceSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenPriceGrainDto>> GetHistoryPriceAsync(string symbol, string dateTime);

}