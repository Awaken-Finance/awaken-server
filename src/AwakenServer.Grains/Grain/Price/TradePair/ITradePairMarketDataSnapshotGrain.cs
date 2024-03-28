using Nethereum.Util;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public interface ITradePairMarketDataSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> GetAsync();
    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AddOrUpdateAsync(TradePairMarketDataSnapshotGrainDto dto);

    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> UpdateTotalSupplyWithLiquidityAsync(
        TradePairMarketDataSnapshotGrainDto dto,
        TradePairMarketDataSnapshotGrainDto latestBeforeDto,
        BigDecimal lpTokenAmount,
        int userTradeAddressCount,
        string lpTokenCurrentSupply);

    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> UpdateTotalSupplyAsync(BigDecimal lpTokenAmount, string lpTokenCurrentSupply,
        DateTime snapshotTime);

    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> UpdateTradeRecord(TradePairMarketDataSnapshotGrainDto dto,
        TradePairMarketDataSnapshotGrainDto lastMarketData);

    Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> UpdateLiquidityWithSyncEvent(TradePairMarketDataSnapshotGrainDto dto,
        TradePairMarketDataSnapshotGrainDto lastMarketData,
        int userTradeAddressCount);
}