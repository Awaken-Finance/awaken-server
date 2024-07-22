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
            State.Address = liquidityRecordDto.Address;
        }
        if (State.IsDeleted)
        {
            InitGrain();
            State.IsDeleted = false;
        }

        State.LastUpdateTime = DateTimeHelper.FromUnixTimeMilliseconds(liquidityRecordDto.Timestamp);
        if (State.LpTokenAmount > 0)
        {
            var holdingTime = (State.LastUpdateTime - State.AverageHoldingStartTime).TotalSeconds;
            var averageHoldingTime = 1.0 * State.LpTokenAmount / (State.LpTokenAmount + liquidityRecordDto.LpTokenAmount) * holdingTime;
            State.AverageHoldingStartTime = State.LastUpdateTime.AddSeconds(-averageHoldingTime);
        }
        else
        {
            State.AverageHoldingStartTime = State.LastUpdateTime;
        }
        State.LpTokenAmount += liquidityRecordDto.LpTokenAmount;
        var isTokenReversed = tradePair.Token0.Symbol != liquidityRecordDto.Token0;
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
            State.Address = liquidityRecordDto.Address;
        }
        var removePercent = (double)liquidityRecordDto.LpTokenAmount / State.LpTokenAmount;
        var receivedToken0TotalFee = (long)(removePercent * State.Token0UnReceivedFee);
        var receivedToken1TotalFee = (long)(removePercent * State.Token1UnReceivedFee);
        State.Token0UnReceivedFee -= receivedToken0TotalFee;
        State.Token1UnReceivedFee -= receivedToken1TotalFee;
        State.Token0ReceivedFee += receivedToken0TotalFee;
        State.Token1ReceivedFee += receivedToken1TotalFee;
        
        var isTokenReversed = tradePair.Token0.Symbol != liquidityRecordDto.Token0;
        State.Token0CumulativeAddition -= isTokenReversed
            ? liquidityRecordDto.Token1Amount
            : liquidityRecordDto.Token0Amount;
        State.Token1CumulativeAddition -= isTokenReversed
            ? liquidityRecordDto.Token0Amount
            : liquidityRecordDto.Token1Amount;
        State.LpTokenAmount -= liquidityRecordDto.LpTokenAmount;
        State.LastUpdateTime = DateTimeHelper.FromUnixTimeMilliseconds(liquidityRecordDto.Timestamp);
        if (State.LpTokenAmount == 0)
        {
            State.IsDeleted = true;
        }

        await WriteStateAsync();
        return new GrainResultDto<CurrentUserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State)
        };
    }

    private void InitGrain()
    {
        State.LpTokenAmount = 0;
        State.Token0UnReceivedFee = 0;
        State.Token1UnReceivedFee = 0;
        State.Token0ReceivedFee = 0;
        State.Token1ReceivedFee = 0;
        State.Token0CumulativeAddition = 0;
        State.Token1CumulativeAddition = 0;
    }

    public async Task<GrainResultDto<CurrentUserLiquidityGrainDto>> AddTotalFee(long total0Fee, long total1Fee, SwapRecordDto swapRecordDto)
    {
        State.Token0UnReceivedFee += total0Fee;
        State.Token1UnReceivedFee += total1Fee;
        State.LastUpdateTime = DateTimeHelper.FromUnixTimeMilliseconds(swapRecordDto.Timestamp);
        await WriteStateAsync();
        return new GrainResultDto<CurrentUserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>(State)
        };
    }
}