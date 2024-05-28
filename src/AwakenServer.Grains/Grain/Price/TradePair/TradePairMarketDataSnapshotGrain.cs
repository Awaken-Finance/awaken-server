using System;
using AwakenServer.Grains.State.Price;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Util;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public class TradePairMarketDataSnapshotGrain : Grain<TradePairMarketDataSnapshotState>,
    ITradePairMarketDataSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TradePairMarketDataSnapshotGrain> _logger;


    public TradePairMarketDataSnapshotGrain(IObjectMapper objectMapper,
        ILogger<TradePairMarketDataSnapshotGrain> logger)
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

    public async Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> GetAsync()
    {
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>()
            {
                Success = false
            };
        }

        return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TradePairMarketDataSnapshotState, TradePairMarketDataSnapshotGrainDto>(State)
        };
    }

    public bool IsInitialSnapshotInTimeRange(TradePairMarketDataSnapshotGrainDto dto,
        TradePairMarketDataSnapshotGrainDto latestBeforeDto)
    {
        return latestBeforeDto.Timestamp != dto.Timestamp;
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AccumulateTotalSupplyAsync(BigDecimal supply)
    {
        State.TotalSupply = (BigDecimal.Parse(State.TotalSupply) + supply).ToNormalizeString();

        await WriteStateAsync();

        return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TradePairMarketDataSnapshotState, TradePairMarketDataSnapshotGrainDto>(State)
        };
    }


    public async Task InitNewAsync(
        TradePairMarketDataSnapshotGrainDto dto,
        TradePairMarketDataSnapshotGrainDto lastDto)
    {
        _logger.LogInformation($"new snapshot dto, trade pair: {dto.TradePairId}, total supply: {dto.TotalSupply}");

        if (dto.TotalSupply == "0" || dto.TotalSupply.IsNullOrEmpty())
        {
            dto.TotalSupply = (BigDecimal.Parse(lastDto.TotalSupply) + BigDecimal.Parse(dto.LpAmount))
                .ToNormalizeString();
        }

        if (dto.Price > 0)
        {
            dto.PriceHigh = dto.Price;
            dto.PriceLow = dto.Price;
        }
        else
        {
            dto.Price = lastDto.Price;
        }

        if (dto.PriceUSD > 0)
        {
            dto.PriceHighUSD = dto.PriceUSD;
            dto.PriceLowUSD = dto.PriceUSD;
        }
        else
        {
            dto.PriceUSD = lastDto.PriceUSD;
        }

        if (dto.TVL <= 0)
        {
            dto.TVL = lastDto.TVL;
        }

        if (dto.ValueLocked0 <= 0)
        {
            dto.ValueLocked0 = lastDto.ValueLocked0;
        }

        if (dto.ValueLocked1 <= 0)
        {
            dto.ValueLocked1 = lastDto.ValueLocked1;
        }

        if (dto.TradeAddressCount24h <= 0)
        {
            dto.TradeAddressCount24h = lastDto.TradeAddressCount24h;
        }

        State =
            _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotState>(dto);

        _logger.LogInformation(
            $"new snapshot state, trade pair: {State.TradePairId}, total supply: {State.TotalSupply}");
    }

    public async Task UpdateLastAsync(
        TradePairMarketDataSnapshotGrainDto updateDto,
        TradePairMarketDataSnapshotGrainDto lastDto)
    {
        _logger.LogInformation(
            $"update snapshot dto, trade pair: {updateDto.TradePairId}, total supply: {updateDto.TotalSupply}");

        if (updateDto.TotalSupply == "0" || updateDto.TotalSupply.IsNullOrEmpty())
        {
            lastDto.TotalSupply = (BigDecimal.Parse(lastDto.TotalSupply) + BigDecimal.Parse(updateDto.LpAmount))
                .ToNormalizeString();
        }
        else
        {
            lastDto.TotalSupply = updateDto.TotalSupply;
        }

        if (updateDto.Volume != 0)
        {
            lastDto.Volume += updateDto.Volume;
        }

        if (updateDto.TradeValue != 0)
        {
            lastDto.TradeValue += updateDto.TradeValue;
        }

        if (updateDto.TradeCount != 0)
        {
            lastDto.TradeCount += updateDto.TradeCount;
        }

        if (updateDto.TradeAddressCount24h > 0)
        {
            lastDto.TradeAddressCount24h = updateDto.TradeAddressCount24h;
        }

        if (updateDto.Price > 0)
        {
            lastDto.Price = updateDto.Price;
            lastDto.PriceHigh = Math.Max(lastDto.PriceHigh, updateDto.Price);
            lastDto.PriceLow = lastDto.PriceLow == 0
                ? updateDto.Price
                : Math.Min(lastDto.PriceLow, updateDto.Price);
        }

        if (updateDto.PriceUSD > 0)
        {
            lastDto.PriceUSD = updateDto.PriceUSD;
            lastDto.PriceHighUSD = Math.Max(lastDto.PriceHighUSD, updateDto.PriceUSD);
            lastDto.PriceLowUSD = lastDto.PriceLowUSD == 0
                ? updateDto.Price
                : Math.Min(lastDto.PriceLowUSD, updateDto.PriceUSD);
        }

        if (updateDto.TVL > 0)
        {
            lastDto.TVL = updateDto.TVL;
        }

        if (updateDto.ValueLocked0 > 0)
        {
            lastDto.ValueLocked0 = updateDto.ValueLocked0;
        }

        if (updateDto.ValueLocked1 > 0)
        {
            lastDto.ValueLocked1 = updateDto.ValueLocked1;
        }

        State =
            _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotState>(lastDto);

        _logger.LogInformation(
            $"update snapshot state, trade pair: {State.TradePairId}, total supply: {State.TotalSupply}");
    }


    public async Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AddOrUpdateAsync(
        TradePairMarketDataSnapshotGrainDto updateDto,
        TradePairMarketDataSnapshotGrainDto lastDto)
    {
        if (updateDto.Id == Guid.Empty)
        {
            updateDto.Id = Guid.NewGuid();
        }

        if (lastDto != null)
        {
            bool initialSnapshot = IsInitialSnapshotInTimeRange(updateDto, lastDto);
            if (initialSnapshot)
            {
                await InitNewAsync(updateDto, lastDto);
            }
            else
            {
                await UpdateLastAsync(updateDto, lastDto);
            }
        }
        else
        {
            if (updateDto.TotalSupply == "0" || updateDto.TotalSupply.IsNullOrEmpty())
            {
                updateDto.TotalSupply = updateDto.LpAmount;
            }

            State = _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotState>(updateDto);
        }

        _logger.LogInformation("UpdateTotalSupplyAsync: totalSupply: {supply}", State.TotalSupply);

        await WriteStateAsync();

        return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TradePairMarketDataSnapshotState, TradePairMarketDataSnapshotGrainDto>(State)
        };
    }
}