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

[GenerateSerializer]
public class TradePairMarketDataSnapshotUpdateResult
{
    [Id(0)]
    public TradePairGrainDto TradePairDto;
    [Id(1)]
    public TradePairMarketDataSnapshotGrainDto SnapshotDto;
    [Id(2)]
    public TradePairMarketDataSnapshotGrainDto LatestSnapshotDto;
}

public interface ITradePairGrain : IGrainWithStringKey
{
    public Task<GrainResultDto<TradePairGrainDto>> GetAsync();
    
    public Task<GrainResultDto<TradePairGrainDto>> AddOrUpdateAsync(TradePairGrainDto dto);
    
    public Task<GrainResultDto<TradePairGrainDto>> UpdateAsync(DateTime timestamp, int userTradeAddressCount, string totalSupply, double token0PriceInUsd, double token1PriceInUsd);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdatePriceAsync(SyncRecordGrainDto dto);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTotalSupplyAsync(LiquidityRecordGrainDto dto);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTradeRecordAsync(TradeRecordGrainDto dto, int tradeAddressCount24h);
    
    public Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> AddOrUpdateSnapshotAsync(TradePairMarketDataSnapshotGrainDto snapshotDto);
    
    public Task<TradePairMarketDataSnapshotGrainDto> GetLatestSnapshotAsync();
    
}