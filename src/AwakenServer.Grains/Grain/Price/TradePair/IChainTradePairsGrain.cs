using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Orleans;

namespace AwakenServer.Grains.Grain.Price;

public interface IChainTradePairsGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ChainTradePairsGrainDto>> AddOrUpdateAsync(ChainTradePairsGrainDto dto);
    Task<GrainResultDto<List<TradePairGrainDto>>> GetAsync();
    Task<GrainResultDto<List<TradePairGrainDto>>> GetAsync(IEnumerable<string> addresses);
    Task<GrainResultDto<TradePairGrainDto>> GetAsync(string address);
}