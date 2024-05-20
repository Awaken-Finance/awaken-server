using AwakenServer.Grains.State.Trade;
using Orleans;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Trade;

public class SyncRecordGrain : Grain<SyncRecordsState>, ISyncRecordGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<LiquidityRecordGrain> _logger;
    public SyncRecordGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient,
        ILogger<LiquidityRecordGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public async Task AddAsync(SyncRecordsGrainDto dto)
    {
        State = _objectMapper.Map<SyncRecordsGrainDto, SyncRecordsState>(dto);
        await WriteStateAsync();
    }

    public async Task<GrainResultDto<SyncRecordsGrainDto>> GetAsync()
    {
        return new GrainResultDto<SyncRecordsGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<SyncRecordsState, SyncRecordsGrainDto>(State)
        };
    }

    public async Task<bool> ExistAsync()
    {
        return State.PairAddress != null;
    }
}