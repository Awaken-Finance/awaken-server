using AwakenServer.Grains.Grain.Price.TradeRecord;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public interface ITradeRecordGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<TradeRecordGrainDto>> InsertAsync(TradeRecordGrainDto dto);
    Task<GrainResultDto<TradeRecordGrainDto>> GetAsync();
}