using System.Threading.Tasks;
using Orleans;

namespace AwakenServer.Grains.Grain.Trade;

public interface ISyncRecordGrain : IGrainWithStringKey
{
    Task AddAsync(SyncRecordsGrainDto dto);
    
    Task<GrainResultDto<SyncRecordsGrainDto>> GetAsync();
    Task<bool> ExistAsync();
}

