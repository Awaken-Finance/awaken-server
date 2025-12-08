using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

public interface ITradeRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TradeRecordGrainDto>> InsertAsync(TradeRecordGrainDto dto);
    Task<GrainResultDto<TradeRecordGrainDto>> GetAsync();
    Task<bool> Exist();
}