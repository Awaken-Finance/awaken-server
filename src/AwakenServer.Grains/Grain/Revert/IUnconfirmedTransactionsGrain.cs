using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Common;
using Orleans;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

public interface IUnconfirmedTransactionsGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UnconfirmedTransactionsGrainDto>> AddAsync(UnconfirmedTransactionsGrainDto dto);
    Task<GrainResultDto<List<UnconfirmedTransactionsGrainDto>>> GetAsync(EventType type, long startBlock, long endBlock);
    Task<long> GetMinUnconfirmedHeightAsync();
}
