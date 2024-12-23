using System.Reflection;
using AwakenServer.Grains.State.MyPortfolio;
using AwakenServer.Grains.State.Trade;
using Orleans;
using Orleans.Core;
using Serilog;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public class CurrentTradePairGrain : Grain<CurrentTradePairState>, ICurrentTradePairGrain
{
    private readonly IObjectMapper _objectMapper;
    public CurrentTradePairGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalSupplyAsync(Guid tradePairId, long lpTokenAmount, long timestamp)
    {
        if (State.TradePairId == Guid.Empty)
        {
            State.TradePairId = tradePairId;
        }
        State.TotalSupply += lpTokenAmount;
        State.LastUpdateTime = DateTimeHelper.FromUnixTimeMilliseconds(timestamp);
        await WriteStateAsync();
        return new GrainResultDto<CurrentTradePairGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentTradePairState, CurrentTradePairGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalFeeAsync(Guid tradePairId, long total0Fee, long total1Fee)
    {
        if (State.TradePairId == Guid.Empty)
        {
            State.TradePairId = tradePairId;
        }
        State.LastUpdateTime = DateTime.UtcNow;
        State.Token0TotalFee += total0Fee;
        State.Token1TotalFee += total1Fee;
        await WriteStateAsync();
        return new GrainResultDto<CurrentTradePairGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentTradePairState, CurrentTradePairGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<CurrentTradePairGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        var result = new GrainResultDto<CurrentTradePairGrainDto>();
        if (State.TradePairId == Guid.Empty)
        {
            result.Success = false;
            return result;
        }
        result.Data = _objectMapper.Map<CurrentTradePairState, CurrentTradePairGrainDto>(State);
        result.Success = true;
        return result;
    }
}