using System.Threading.Tasks;
using Nethereum.Util;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public interface ITradePairMarketDataSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> GetAsync();
    
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AddOrUpdateAsync(
        TradePairMarketDataSnapshotGrainDto updateDto,
        TradePairMarketDataSnapshotGrainDto lastDto);
    
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AlignAsync(
        TradePairMarketDataSnapshotGrainDto updateDto);
    
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AccumulateTotalSupplyAsync(BigDecimal supply);
    
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> UpdateTotalSupplyAsync(string supply);
}