using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.State.Price;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public class TradeRecordGrain : Grain<TradeRecordState>, ITradeRecordGrain
{
    private readonly IObjectMapper _objectMapper;

    public TradeRecordGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
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

    public async Task<GrainResultDto<TradeRecordGrainDto>> InsertAsync(TradeRecordGrainDto dto)
    {
        State = _objectMapper.Map<TradeRecordGrainDto, TradeRecordState>(dto);
        await WriteStateAsync();

        return new GrainResultDto<TradeRecordGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }
    
    public async Task<GrainResultDto<TradeRecordGrainDto>> GetAsync()
    {
        var dto = _objectMapper.Map<TradeRecordState, TradeRecordGrainDto>(State);
        return new GrainResultDto<TradeRecordGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }
}