using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.State.Trade;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Trade;

public class LiquidityRecordGrain : Grain<LiquidityRecordState>, ILiquidityRecordGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<LiquidityRecordGrain> _logger;
    public LiquidityRecordGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient,
        ILogger<LiquidityRecordGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    
    public async Task AddAsync(LiquidityRecordGrainDto liquidityRecord)
    {
        State = _objectMapper.Map<LiquidityRecordGrainDto, LiquidityRecordState>(liquidityRecord);
        await WriteStateAsync();
    }

    public async Task<GrainResultDto<LiquidityRecordGrainDto>> GetAsync()
    {
        return new GrainResultDto<LiquidityRecordGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<LiquidityRecordState, LiquidityRecordGrainDto>(State)
        };
    }

    public async Task<bool> ExistAsync()
    {
        return State.TransactionHash != null;
    }
}