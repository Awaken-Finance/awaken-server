using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradePair;
public class TradePairMarketDataSnapshotUpdateResult
{
    public TradePairGrainDto TradePairDto;
    public TradePairMarketDataSnapshotGrainDto SnapshotDto;
    public TradePairMarketDataSnapshotGrainDto LatestSnapshotDto;
}

public interface ITradePairGrain : IGrainWithStringKey
{
    public Task<GrainResultDto<TradePairGrainDto>> GetAsync();
    
    public Task<GrainResultDto<TradePairGrainDto>> AddOrUpdateAsync(TradePairGrainDto dto);
    
    public Task<GrainResultDto<TradePairGrainDto>> UpdateAsync(DateTime timestamp, int userTradeAddressCount, string totalSupply);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTotalSupplyAsync(string totalSupply);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdatePriceAsync(SyncRecordGrainDto dto);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> AlignPriceAsync24h(List<SyncRecordGrainDto> dtos);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTotalSupplyAsync(LiquidityRecordGrainDto dto);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTradeRecordAsync(TradeRecordGrainDto dto, int tradeAddressCount24h);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> AddOrUpdateSnapshotAsync(TradePairMarketDataSnapshotGrainDto snapshotDto);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AlignSnapshotAsync(TradePairMarketDataSnapshotGrainDto snapshotDto);
    
    public Task<TradePairMarketDataSnapshotGrainDto> GetLatestSnapshotAsync();
    
}