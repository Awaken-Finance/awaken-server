using AwakenServer.Grains.State.MyPortfolio;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public class CurrentUserLiquidityGrain : Grain<CurrentUserLiquidityState>, ICurrentUserLiquidityGrain
{
    private readonly IObjectMapper _objectMapper;
    public CurrentUserLiquidityGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<CurrentUserLiquidityGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        var result = new GrainResultDto<CurrentUserLiquidityGrainDto>();
        if (State.TradePairId == Guid.Empty || State.IsDeleted)
        {
            result.Success = false;
            return result;
        }
        result.Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State);
        result.Success = true;
        return result;
    }

    public async Task<GrainResultDto<CurrentUserLiquidityGrainDto>> AddLiquidityAsync(TradePair tradePair, LiquidityRecordDto liquidityRecordDto)
    {
        if (State.TradePairId == Guid.Empty)
        {
            State.TradePairId = tradePair.Id;
            State.Address = liquidityRecordDto.To;
        }
        State.LastUpdateTime = DateTime.UtcNow;
        if (State.LpTokenAmount > 0)
        {
            var holdingHours = (long)(State.LastUpdateTime - State.AverageHoldingStartTime).TotalHours;
            var averageHoldingHours = 1.0 * holdingHours * State.LpTokenAmount / (State.LpTokenAmount + liquidityRecordDto.LpTokenAmount);
            State.AverageHoldingStartTime = State.LastUpdateTime.AddHours(-averageHoldingHours);
        }
        else
        {
            State.AverageHoldingStartTime = State.LastUpdateTime;
        }
        State.LpTokenAmount += liquidityRecordDto.LpTokenAmount;
        var isTokenReversed = tradePair.Token0.Symbol == liquidityRecordDto.Token0;
        State.Token0CumulativeAddition +=
            isTokenReversed ? liquidityRecordDto.Token1Amount : liquidityRecordDto.Token0Amount;
        State.Token1CumulativeAddition +=
            isTokenReversed ? liquidityRecordDto.Token0Amount : liquidityRecordDto.Token1Amount;
        await WriteStateAsync();
        return new GrainResultDto<CurrentUserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<CurrentUserLiquidityGrainDto>> RemoveLiquidityAsync(TradePair tradePair, LiquidityRecordDto liquidityRecordDto)
    {
        if (State.TradePairId == Guid.Empty)
        {
            State.TradePairId = tradePair.Id;
            State.Address = liquidityRecordDto.To;
        }
        var removePercent = (double)liquidityRecordDto.LpTokenAmount / State.LpTokenAmount;
        var receivedToken0TotalFee = (long)(removePercent * State.Token0UnReceivedFee);
        var receivedToken1TotalFee = (long)(removePercent * State.Token1UnReceivedFee);
        State.Token0UnReceivedFee -= receivedToken0TotalFee;
        State.Token1UnReceivedFee -= receivedToken1TotalFee;
        State.Token0ReceivedFee += receivedToken0TotalFee;
        State.Token1ReceivedFee += receivedToken1TotalFee;
        
        var isTokenReversed = tradePair.Token0.Symbol == liquidityRecordDto.Token0;
        State.Token0CumulativeAddition -= (isTokenReversed
            ? liquidityRecordDto.Token1Amount
            : liquidityRecordDto.Token0Amount) - receivedToken0TotalFee;
        State.Token1CumulativeAddition -= (isTokenReversed
            ? liquidityRecordDto.Token0Amount
            : liquidityRecordDto.Token1Amount) - receivedToken1TotalFee;
        State.LpTokenAmount -= liquidityRecordDto.LpTokenAmount;
        await WriteStateAsync();
        return new GrainResultDto<CurrentUserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<CurrentUserLiquidityGrainDto>> AddTotalFee(long total0Fee, long total1Fee)
    {
        State.Token0UnReceivedFee += total0Fee;
        State.Token1UnReceivedFee += total1Fee;
        await WriteStateAsync();
        return new GrainResultDto<CurrentUserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State)
        };
    }
}