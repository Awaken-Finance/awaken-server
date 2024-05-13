using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Price;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using Nest;
using Nethereum.Util;
using Orleans;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public class TradePairGrain : Grain<TradePairState>, ITradePairGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TradePairGrain> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly SortedDictionary<DateTime, string> _previous7DaysMarketDataSnapshots;

    public TradePairGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient,
        ILogger<TradePairGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _previous7DaysMarketDataSnapshots = new SortedDictionary<DateTime, string>(
            Comparer<DateTime>.Create((datetime1, datetime2) => { return datetime2.CompareTo(datetime1); })
        );
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await LoadPastWeekSnapshots();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    private async Task LoadPastWeekSnapshots()
    {
        DateTime now = DateTime.Now;
        DateTime pastWeek = now.AddDays(-7);

        foreach (var snapshotId in State.MarketDataSnapshotGrainIds)
        {
            ITradePairMarketDataSnapshotGrain snapshotGrain =
                _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshotId);
            var snapshotDataResult = await snapshotGrain.GetAsync();
            if (snapshotDataResult.Success)
            {
                var snapshotData = snapshotDataResult.Data;
                if (snapshotData.Timestamp >= pastWeek && snapshotData.Timestamp <= now)
                {
                    _previous7DaysMarketDataSnapshots.Add(snapshotData.Timestamp,
                        snapshotGrain.GetPrimaryKeyString());
                }
            }
        }
    }

    public async Task<GrainResultDto<TradePairGrainDto>> GetAsync()
    {
        if (State.Id == Guid.Empty || State.IsDeleted)
        {
            return new GrainResultDto<TradePairGrainDto>
            {
                Success = false
            };
        }

        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
        };
    }

    public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestBeforeSnapshotAsync(DateTime maxTime)
    {
        foreach (var snapshot in _previous7DaysMarketDataSnapshots)
        {
            if (maxTime >= snapshot.Key)
            {
                var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
                var result = await grain.GetAsync();
                if (result.Success)
                {
                    return result.Data;
                }
                else
                {
                    _logger.LogError($"trade pair {State.Id} contains empty snapshot grain");
                }
            }
        }

        return null;
    }

    public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestSnapshotAsync()
    {
        if (_previous7DaysMarketDataSnapshots.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var snapshot in _previous7DaysMarketDataSnapshots)
        {
            var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
            var result = await grain.GetAsync();
            if (result.Success)
            {
                return result.Data;
            }
        }

        return null;
    }

    public async Task<ITradePairMarketDataSnapshotGrain> GetLatestSnapshotGrainAsync()
    {
        if (_previous7DaysMarketDataSnapshots.IsNullOrEmpty())
        {
            return null;
        }

        return _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(_previous7DaysMarketDataSnapshots
            .FirstOrDefault().Value);
    }

    public async Task<List<TradePairMarketDataSnapshotGrainDto>> GetPrevious7DaysSnapshotsDtoAsync()
    {
        List<TradePairMarketDataSnapshotGrainDto> sortedSnapshots = new List<TradePairMarketDataSnapshotGrainDto>();
        foreach (var snapshot in _previous7DaysMarketDataSnapshots)
        {
            var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
            var result = await grain.GetAsync();
            if (result.Success)
            {
                sortedSnapshots.Add(result.Data);
            }
        }

        return sortedSnapshots;
    }

    public DateTime GetSnapshotTime(DateTime time)
    {
        return time.Date.AddHours(time.Hour);
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTradeRecordAsync(
        TradeRecordGrainDto dto, int tradeAddressCount24h)
    {
        return await AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
        {
            Id = Guid.NewGuid(),
            ChainId = dto.ChainId,
            TradePairId = State.Id,
            Volume = dto.IsRevert ? -double.Parse(dto.Token0Amount) : double.Parse(dto.Token0Amount),
            TradeValue = dto.IsRevert ? -double.Parse(dto.Token1Amount) : double.Parse(dto.Token1Amount),
            TradeCount = dto.IsRevert ? -1:1,
            Timestamp = GetSnapshotTime(dto.Timestamp),
            TradeAddressCount24h = tradeAddressCount24h
        });
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>>
        UpdateTotalSupplyAsync(
            LiquidityRecordGrainDto dto)
    {
        var lpAmount = BigDecimal.Parse(dto.LpTokenAmount);
        lpAmount = dto.Type == LiquidityType.Mint ? lpAmount : -lpAmount;
        lpAmount = dto.IsRevert ? -lpAmount : lpAmount;
        
        var updateResult = await AddOrUpdateSnapshotAsync(
            new TradePairMarketDataSnapshotGrainDto
            {
                Id = Guid.NewGuid(),
                ChainId = dto.ChainId,
                TradePairId = State.Id,
                Timestamp = GetSnapshotTime(dto.Timestamp),
                TotalSupply = lpAmount.ToNormalizeString(),
            });

        // nie:The current snapshot is not up-to-date. The latest snapshot needs to update TotalSupply 
        var latestSnapshot = await GetLatestSnapshotAsync();
        if (latestSnapshot != null && updateResult.Data.SnapshotDto.Timestamp < latestSnapshot.Timestamp)
        {
            var latestGrain = await GetLatestSnapshotGrainAsync();
            var latestResult = await latestGrain.AccumulateTotalSupplyAsync(lpAmount);
            var updateTradePairByLatestResult = await UpdateFromSnapshotAsync(latestResult.Data);
            return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
            {
                Success = true,
                Data = new TradePairMarketDataSnapshotUpdateResult
                {
                    SnapshotDto = updateResult.Data.SnapshotDto,
                    LatestSnapshotDto = latestResult.Data,
                    TradePairDto = updateTradePairByLatestResult.Data
                }
            };
        }

        return updateResult;
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> AlignPriceAsync24h(List<SyncRecordGrainDto> dtos)
    {
        Dictionary<DateTime, Tuple<double, double, double, double>> snapshotPriceHighLow = new Dictionary<DateTime, Tuple<double, double, double, double>>();
        foreach (var dto in dtos)
        {
            var isReversed = State.Token0.Symbol == dto.SymbolB;
            var token0Amount = isReversed
                ? dto.ReserveB.ToDecimalsString(State.Token0.Decimals)
                : dto.ReserveA.ToDecimalsString(State.Token0.Decimals);
            var token1Amount = isReversed
                ? dto.ReserveA.ToDecimalsString(State.Token1.Decimals)
                : dto.ReserveB.ToDecimalsString(State.Token1.Decimals);

            _logger.LogInformation(
                "AlignPriceAsync, input chainId: {chainId}, isReversed: {isReversed}, token0Amount: {token0Amount}, " +
                "token1Amount: {token1Amount}, tradePairId: {tradePairId}, timestamp: {timestamp}, blockHeight: {blockHeight}",
                dto.ChainId,
                isReversed, token0Amount, token1Amount, State.Id, dto.Timestamp, dto.BlockHeight);

            var timestamp = DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp);
            var price = double.Parse(token1Amount) / double.Parse(token0Amount);

            var token0PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token0.Symbol)
                .GetCurrentPriceAsync(State.Token0.Symbol);
            if (!token0PriceResult.Success)
            {
                _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token0.Symbol}");
            }

            var token1PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token1.Symbol)
                .GetCurrentPriceAsync(State.Token1.Symbol);
            if (!token1PriceResult.Success)
            {
                _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token1.Symbol}");
            }

            var priceUSD0 = (double)token0PriceResult.Data.PriceInUsd;
            var priceUSD1 = (double)token1PriceResult.Data.PriceInUsd;
            
            var priceUSD = priceUSD1 != 0 ? price * priceUSD1 : priceUSD0;
            var snapshotTime = GetSnapshotTime(timestamp);
            if (!snapshotPriceHighLow.ContainsKey(snapshotTime))
            {
                snapshotPriceHighLow[snapshotTime] =
                    new Tuple<double, double, double, double>(price, price, priceUSD, priceUSD);
            }
            else
            {
                var priceHigh = Math.Max(snapshotPriceHighLow[snapshotTime].Item1, price);
                var priceLow = snapshotPriceHighLow[snapshotTime].Item2 == 0 ? price : Math.Min(snapshotPriceHighLow[snapshotTime].Item2, price);
                var priceUSDHigh = Math.Max(snapshotPriceHighLow[snapshotTime].Item3, priceUSD);
                var priceUSDLow = snapshotPriceHighLow[snapshotTime].Item4 == 0 ? priceUSD : Math.Min(snapshotPriceHighLow[snapshotTime].Item4, priceUSD);
                snapshotPriceHighLow[snapshotTime] =
                    new Tuple<double, double, double, double>(priceHigh, priceLow, priceUSDHigh, priceUSDLow);
            }
            
        }

        var latestSnapshotResult = new GrainResultDto<TradePairMarketDataSnapshotGrainDto>();
        bool flag = true;
        foreach (var hourSnapshot in snapshotPriceHighLow)
        {
            var result = await AlignSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
            {
                Id = Guid.NewGuid(),
                ChainId = State.ChainId,
                TradePairId = State.Id,
                PriceHigh = hourSnapshot.Value.Item1,
                PriceLow = hourSnapshot.Value.Item2,
                PriceHighUSD = hourSnapshot.Value.Item3,
                PriceLowUSD = hourSnapshot.Value.Item4,
                Timestamp = hourSnapshot.Key,
            });

            if (!result.Success)
            {
                _logger.LogError($"AlignSnapshotAsync failed. trade pair id: {State.Id}");
                return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
                {
                    Success = false
                };    
            }
            
            if (result.Success && flag)
            {
                latestSnapshotResult = result;
                flag = false;
                _logger.LogInformation($"align snapshot, trade pair id: {State.Id} latest time: {latestSnapshotResult.Data.Timestamp}, PriceHigh: {latestSnapshotResult.Data.PriceHigh}, PriceLow: {latestSnapshotResult.Data.PriceLow}, PriceHighUSD: {latestSnapshotResult.Data.PriceHighUSD}, PriceLowUSD: {latestSnapshotResult.Data.PriceLowUSD}");
            } 
            else if (result.Success && result.Data.Timestamp > latestSnapshotResult.Data.Timestamp)
            {
                latestSnapshotResult = result;
                _logger.LogInformation($"align snapshot, trade pair id: {State.Id} latest time: {latestSnapshotResult.Data.Timestamp}, PriceHigh: {latestSnapshotResult.Data.PriceHigh}, PriceLow: {latestSnapshotResult.Data.PriceLow}, PriceHighUSD: {latestSnapshotResult.Data.PriceHighUSD}, PriceLowUSD: {latestSnapshotResult.Data.PriceLowUSD}");
            }
        }

        if (latestSnapshotResult.Success)
        {
            var updateTradePairResult = await UpdatePrice24hHighLowAsync(latestSnapshotResult.Data);
            return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
            {
                Success = true,
                Data = new TradePairMarketDataSnapshotUpdateResult
                {
                    TradePairDto = updateTradePairResult.Data,
                    SnapshotDto = latestSnapshotResult.Data
                }
            };
        }
        
        return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
        {
            Success = false
        };
    }
    
    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>>
        UpdatePriceAsync(SyncRecordGrainDto dto)
    {
        var isReversed = State.Token0.Symbol == dto.SymbolB;
        var token0Amount = isReversed
            ? dto.ReserveB.ToDecimalsString(State.Token0.Decimals)
            : dto.ReserveA.ToDecimalsString(State.Token0.Decimals);
        var token1Amount = isReversed
            ? dto.ReserveA.ToDecimalsString(State.Token1.Decimals)
            : dto.ReserveB.ToDecimalsString(State.Token1.Decimals);

        _logger.LogInformation(
            "SyncEvent, input chainId: {chainId}, isReversed: {isReversed}, token0Amount: {token0Amount}, " +
            "token1Amount: {token1Amount}, tradePairId: {tradePairId}, timestamp: {timestamp}, blockHeight: {blockHeight}",
            dto.ChainId,
            isReversed, token0Amount, token1Amount, State.Id, dto.Timestamp, dto.BlockHeight);

        var timestamp = DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp);
        var price = double.Parse(token1Amount) / double.Parse(token0Amount);

        var token0PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token0.Symbol)
            .GetCurrentPriceAsync(State.Token0.Symbol);
        if (!token0PriceResult.Success)
        {
            _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token0.Symbol}");
        }

        var token1PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token1.Symbol)
            .GetCurrentPriceAsync(State.Token1.Symbol);
        if (!token1PriceResult.Success)
        {
            _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token1.Symbol}");
        }

        var priceUSD0 = (double)token0PriceResult.Data.PriceInUsd;
        var priceUSD1 = (double)token1PriceResult.Data.PriceInUsd;

        var tvl = priceUSD0 * double.Parse(token0Amount) +
                  priceUSD1 * double.Parse(token1Amount);

        var priceUSD = priceUSD1 != 0 ? price * priceUSD1 : priceUSD0;

        return await AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
        {
            Id = Guid.NewGuid(),
            ChainId = State.ChainId,
            TradePairId = State.Id,
            Price = price,
            PriceHigh = price,
            PriceLow = price,
            PriceLowUSD = priceUSD,
            PriceHighUSD = priceUSD,
            PriceUSD = priceUSD,
            TVL = tvl,
            ValueLocked0 = double.Parse(token0Amount),
            ValueLocked1 = double.Parse(token1Amount),
            Timestamp = GetSnapshotTime(timestamp),
        });
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotGrainDto>> AlignSnapshotAsync(
        TradePairMarketDataSnapshotGrainDto snapshotDto)
    {
        if (State.Id == Guid.Empty || State.Token0 == null || State.Token1 == null)
        {
            _logger.LogError($"add snapshot to an error trade pair, id: {snapshotDto.TradePairId}, " +
                             $"timestamp: {snapshotDto.Timestamp}");
            return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>
            {
                Success = false
            };
        }

        snapshotDto.Timestamp = GetSnapshotTime(snapshotDto.Timestamp);

        _logger.LogInformation(
            $"align snapshot id:{State.Id},{State.Token0.Symbol}-{State.Token1.Symbol}, " +
            $"timestamp:{snapshotDto.Timestamp} " +
            $"fee:{State.FeeRate},price:{State.Price}-priceUSD:{State.PriceUSD}, " +
            $"tvl:{State.TVL}");

        var snapshotGrain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(
            GrainIdHelper.GenerateGrainId(snapshotDto.ChainId, snapshotDto.TradePairId, snapshotDto.Timestamp));

        var snapshotResultDto = await snapshotGrain.GetAsync();
        if (snapshotResultDto.Success)
        {
            // update snapshot grain
            var updateSnapshotResult = await snapshotGrain.AlignAsync(snapshotDto);
        
            return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>
            {
                Success = true,
                Data = updateSnapshotResult.Data
            };
        }
     
        _logger.LogError($"align snapshot price failed, can't find snapshot: {GrainIdHelper.GenerateGrainId(snapshotDto.ChainId, snapshotDto.TradePairId, snapshotDto.Timestamp)}");
        
        return new GrainResultDto<TradePairMarketDataSnapshotGrainDto>
        {
            Success = false
        };
        
    }
    
    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>>
        AddOrUpdateSnapshotAsync(TradePairMarketDataSnapshotGrainDto snapshotDto)
    {
        if (State.Id == Guid.Empty || State.Token0 == null || State.Token1 == null)
        {
            _logger.LogError($"add snapshot to an error trade pair, id: {snapshotDto.TradePairId}, " +
                             $"timestamp: {snapshotDto.Timestamp}");
            return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
            {
                Success = false
            };
        }

        snapshotDto.Timestamp = GetSnapshotTime(snapshotDto.Timestamp);

        _logger.LogInformation(
            $"add snapshot id:{State.Id},{State.Token0.Symbol}-{State.Token1.Symbol}, " +
            $"timestamp:{snapshotDto.Timestamp} " +
            $"fee:{State.FeeRate},price:{State.Price}-priceUSD:{State.PriceUSD}, " +
            $"tvl:{State.TVL}");

        var snapshotGrain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(
            GrainIdHelper.GenerateGrainId(snapshotDto.ChainId, snapshotDto.TradePairId, snapshotDto.Timestamp));

        // update snapshot grain
        var latestBeforeDto = await GetLatestBeforeSnapshotAsync(snapshotDto.Timestamp);
        var updateSnapshotResult = await snapshotGrain.AddOrUpdateAsync(snapshotDto, latestBeforeDto);

        // add snapshot
        if (!State.MarketDataSnapshotGrainIds.Contains(snapshotGrain.GetPrimaryKeyString()))
        {
            if (_previous7DaysMarketDataSnapshots.Count >= 7 * 24)
            {
                var lastKey = _previous7DaysMarketDataSnapshots.Last().Key;
                bool removed = _previous7DaysMarketDataSnapshots.Remove(lastKey);
                if (!removed)
                {
                    _logger.LogError("previous 7 days market data snapshots remove failed");
                }
            }

            _previous7DaysMarketDataSnapshots.Add(snapshotDto.Timestamp,
                snapshotGrain.GetPrimaryKeyString());
            State.MarketDataSnapshotGrainIds.Add(snapshotGrain.GetPrimaryKeyString());
        }
        
        // update trade pair
        var updateTradePairResult = await UpdateFromSnapshotAsync(updateSnapshotResult.Data);
        return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
        {
            Success = true,
            Data = new TradePairMarketDataSnapshotUpdateResult
            {
                TradePairDto = updateTradePairResult.Data,
                SnapshotDto = updateSnapshotResult.Data
            }
        };
    }

    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> UpdateTotalSupplyAsync(string totalSupply)
    {
        State.TotalSupply = totalSupply;
        
        _logger.LogInformation($"update trade pair total supply, id: {State.Id}, address: {State.Address}, total supply: {totalSupply}");

        await WriteStateAsync();
        
        var latestSnapshotGrain = await GetLatestSnapshotGrainAsync();
        if (latestSnapshotGrain != null)
        {
            var updateSnapshotResult = await latestSnapshotGrain.UpdateTotalSupplyAsync(totalSupply);
            return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
            {
                Success = true,
                Data = new TradePairMarketDataSnapshotUpdateResult
                {
                    TradePairDto = _objectMapper.Map<TradePairState, TradePairGrainDto>(State),
                    SnapshotDto = updateSnapshotResult.Data
                }
            };
        }
        
        return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
        {
            Success = true,
            Data = new TradePairMarketDataSnapshotUpdateResult
            {
                TradePairDto = _objectMapper.Map<TradePairState, TradePairGrainDto>(State),
            }
        };
    }

    public async Task<GrainResultDto<TradePairGrainDto>> UpdateAsync(DateTime timestamp,
        int userTradeAddressCount,
        string totalSupply)
    {
        _logger.LogInformation($"Scheduled trade pair update begin, id: {State.Id}, " +
                               $"timestamp: {timestamp}, " +
                               $"current trade pair: {JsonConvert.SerializeObject(State)}");
        
        var previous7DaysSnapshotDtos = await GetPrevious7DaysSnapshotsDtoAsync();

        var volume24h = 0d;
        var tradeValue24h = 0d;
        var tradeCount24h = 0;
        var priceHigh24h = State.Price;
        var priceLow24h = State.Price;
        var priceHigh24hUSD = State.PriceUSD;
        var priceLow24hUSD = State.PriceUSD;
        var daySnapshot = previous7DaysSnapshotDtos.Where(s => s.Timestamp >= timestamp.AddDays(-1)).ToList();
        var snaoshotCount = 0;
        foreach (var snapshot in daySnapshot)
        {
            _logger.LogInformation($"Scheduled trade pair update, " +
                                   $"daySnapshot : {snaoshotCount}, " +
                                   $"snapshot: {snapshot}");
            snaoshotCount++;
            volume24h += snapshot.Volume;
            tradeValue24h += snapshot.TradeValue;
            tradeCount24h += snapshot.TradeCount;
            priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
            priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
            priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
            priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
        }

        var lastDaySnapshot = previous7DaysSnapshotDtos
            .Where(s => s.Timestamp >= timestamp.AddDays(-2) && s.Timestamp < timestamp.AddDays(-1))
            .OrderByDescending(s => s.Timestamp).ToList();
        
        var lastDayVolume24h = lastDaySnapshot.Sum(snapshot => snapshot.Volume);
        var lastDayTvl = 0d;
        var lastDayPriceUSD = 0d;
        if (lastDaySnapshot.Count > 0)
        {
            var snapshot = lastDaySnapshot.First();
            lastDayTvl = snapshot.TVL;
            lastDayPriceUSD = snapshot.PriceUSD;
        }
        else
        {
            var sortDaySnapshot = daySnapshot.OrderBy(s => s.Timestamp).ToList();
            if (sortDaySnapshot.Count > 0)
            {
                var snapshot = sortDaySnapshot.First();
                lastDayTvl = snapshot.TVL;
                lastDayPriceUSD = snapshot.PriceUSD;
            }
        }
        
        _logger.LogInformation($"Scheduled trade pair update, " +
                               $"lastDaySnapshot count: {lastDaySnapshot.Count}, " +
                               $"lastDayVolume24h: {lastDayVolume24h}, " +
                               $"lastDayTvl: {lastDayTvl}, " +
                               $"lastDayPriceUSD: {lastDayPriceUSD}");

        var token0PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token0.Symbol)
            .GetCurrentPriceAsync(State.Token0.Symbol);
        if (!token0PriceResult.Success)
        {
            _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token0.Symbol}");
        }

        var token1PriceResult = await _clusterClient.GetGrain<ITokenPriceGrain>(State.Token1.Symbol)
            .GetCurrentPriceAsync(State.Token1.Symbol);
        if (!token1PriceResult.Success)
        {
            _logger.LogError($"get token price from token price grain failed. token symbol: {State.Token1.Symbol}");
        }

        var priceUSD0 = (double)token0PriceResult.Data.PriceInUsd;
        var priceUSD1 = (double)token1PriceResult.Data.PriceInUsd;

        State.PriceUSD = priceUSD1 != 0 ? State.Price * (double)priceUSD1 : (double)priceUSD0;
        State.PricePercentChange24h = lastDayPriceUSD == 0
            ? 0
            : (State.PriceUSD - lastDayPriceUSD) * 100 / lastDayPriceUSD;
        State.PriceChange24h = lastDayPriceUSD == 0
            ? 0
            : State.PriceUSD - lastDayPriceUSD;
        State.TVL = (double)priceUSD0 * State.ValueLocked0 + (double)priceUSD1 * State.ValueLocked1;
        State.TVLPercentChange24h = lastDayTvl == 0
            ? 0
            : (State.TVL - lastDayTvl) * 100 / lastDayTvl;
        State.PriceHigh24h = priceHigh24h;
        State.PriceHigh24hUSD = priceHigh24hUSD;
        State.PriceLow24hUSD = priceLow24hUSD;
        State.PriceLow24h = priceLow24h;
        State.Volume24h = volume24h;
        State.VolumePercentChange24h = lastDayVolume24h == 0
            ? 0
            : (State.Volume24h - lastDayVolume24h) * 100 / lastDayVolume24h;
        State.TradeValue24h = tradeValue24h;
        State.TradeCount24h = tradeCount24h;

        var volume7d = previous7DaysSnapshotDtos.Sum(k =>
            k.Volume);
        State.FeePercent7d =
            State.TVL == 0 ? 0 : (volume7d * State.PriceUSD * State.FeeRate * 365 * 100) / (State.TVL * 7);
        State.TradeAddressCount24h = userTradeAddressCount;
        State.TotalSupply = totalSupply;
        
        _logger.LogInformation($"Scheduled trade pair update end, id: {State.Id}, " +
                               $"timestamp: {timestamp}, " +
                               $"after update, trade pair: {JsonConvert.SerializeObject(State)}");

        await WriteStateAsync();

        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<TradePairGrainDto>> UpdatePrice24hHighLowAsync(TradePairMarketDataSnapshotGrainDto dto)
    {
        var previous7DaysSnapshotDtos = await GetPrevious7DaysSnapshotsDtoAsync();
        
        var priceHigh24h = dto.PriceHigh;
        var priceLow24h = dto.PriceLow;
        var priceHigh24hUSD = dto.PriceHighUSD;
        var priceLow24hUSD = dto.PriceLowUSD;

        var daySnapshot = previous7DaysSnapshotDtos.Where(s => s.Timestamp > dto.Timestamp.AddDays(-1)).ToList();
        foreach (var snapshot in daySnapshot)
        {
            _logger.LogInformation($"align snapshot, trade pair id: {State.Id} snapshot info, time: {snapshot.Timestamp}, PriceHigh: {snapshot.PriceHigh}, PriceLow: {snapshot.PriceLow}, PriceHighUSD: {snapshot.PriceHighUSD}, PriceLowUSD: {snapshot.PriceLowUSD}");
            
            if (priceLow24h == 0)
            {
                priceLow24h = snapshot.PriceLow;
            }

            if (snapshot.PriceLow != 0)
            {
                priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
            }

            if (priceLow24hUSD == 0)
            {
                priceLow24hUSD = snapshot.PriceLowUSD;
            }

            if (snapshot.PriceLowUSD != 0)
            {
                priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
            }

            priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
            priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
            
            _logger.LogInformation($"align snapshot, current trade pair id: {State.Id} priceHigh24h: {priceHigh24h}, priceLow24h: {priceLow24h}, priceHigh24hUSD: {priceHigh24hUSD}, priceLow24hUSD: {priceLow24hUSD}");
        }
        
        State.PriceHigh24h = priceHigh24h;
        State.PriceLow24h = priceLow24h;
        State.PriceHigh24hUSD = priceHigh24hUSD;
        State.PriceLow24hUSD = priceLow24hUSD;
        
        await WriteStateAsync();

        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<TradePairGrainDto>> UpdateFromSnapshotAsync(
        TradePairMarketDataSnapshotGrainDto dto)
    {
        var latestSnapshot = await GetLatestSnapshotAsync();
        if (latestSnapshot != null && dto.Timestamp < latestSnapshot.Timestamp)
        {
            return new GrainResultDto<TradePairGrainDto>
            {
                Success = true,
                Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
            };
        }

        var previous7DaysSnapshotDtos = await GetPrevious7DaysSnapshotsDtoAsync();
        var tokenAValue24 = 0d;
        var tokenBValue24 = 0d;
        var tradeCount24h = 0;
        var priceHigh24h = dto.PriceHigh;
        var priceLow24h = dto.PriceLow;
        var priceHigh24hUSD = dto.PriceHighUSD;
        var priceLow24hUSD = dto.PriceLowUSD;

        var daySnapshot = previous7DaysSnapshotDtos.Where(s => s.Timestamp >= dto.Timestamp.AddDays(-1)).ToList();
        foreach (var snapshot in daySnapshot)
        {
            tokenAValue24 += snapshot.Volume;
            tokenBValue24 += snapshot.TradeValue;
            tradeCount24h += snapshot.TradeCount;

            if (priceLow24h == 0)
            {
                priceLow24h = snapshot.PriceLow;
            }

            if (snapshot.PriceLow != 0)
            {
                priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
            }

            if (priceLow24hUSD == 0)
            {
                priceLow24hUSD = snapshot.PriceLowUSD;
            }

            if (snapshot.PriceLowUSD != 0)
            {
                priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
            }

            priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
            priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
        }

        var lastDaySnapshot = previous7DaysSnapshotDtos.Where(s => s.Timestamp < dto.Timestamp.AddDays(-1))
            .OrderByDescending(s => s.Timestamp).ToList();
        var lastDayVolume24h = lastDaySnapshot.Sum(snapshot => snapshot.Volume);
        var lastDayTvl = 0d;
        var lastDayPriceUSD = 0d;

        if (lastDaySnapshot.Count > 0)
        {
            var snapshot = lastDaySnapshot.First();
            lastDayTvl = snapshot.TVL;
            lastDayPriceUSD = snapshot.PriceUSD;
        }
        else
        {
            var latestBeforeThisSnapshotDto = await GetLatestBeforeSnapshotAsync(dto.Timestamp);
            if (latestBeforeThisSnapshotDto != null)
            {
                lastDayTvl = latestBeforeThisSnapshotDto.TVL;
                lastDayPriceUSD = latestBeforeThisSnapshotDto.PriceUSD;
            }
        }
        
        State.TotalSupply = dto.TotalSupply;
        State.Price = dto.Price;
        State.PriceUSD = dto.PriceUSD;
        State.TVL = dto.TVL;
        State.ValueLocked0 = dto.ValueLocked0;
        State.ValueLocked1 = dto.ValueLocked1;
        State.Volume24h = tokenAValue24;
        State.TradeValue24h = tokenBValue24;
        State.TradeCount24h = tradeCount24h;
        State.TradeAddressCount24h = dto.TradeAddressCount24h;
        State.PriceHigh24h = priceHigh24h;
        State.PriceLow24h = priceLow24h;
        State.PriceHigh24hUSD = priceHigh24hUSD;
        State.PriceLow24hUSD = priceLow24hUSD;
        State.PriceChange24h = lastDayPriceUSD == 0
            ? 0
            : State.PriceUSD - lastDayPriceUSD;
        State.PricePercentChange24h = lastDayPriceUSD == 0
            ? 0
            : (State.PriceUSD - lastDayPriceUSD) * 100 / lastDayPriceUSD;
        State.VolumePercentChange24h = lastDayVolume24h == 0
            ? 0
            : (State.Volume24h - lastDayVolume24h) * 100 / lastDayVolume24h;
        State.TVLPercentChange24h = lastDayTvl == 0
            ? 0
            : (State.TVL - lastDayTvl) * 100 / lastDayTvl;

        if (dto.TVL != 0)
        {
            var volume7d = previous7DaysSnapshotDtos
                .Sum(k => k.Volume);
            volume7d += dto.Volume;
            State.FeePercent7d = (volume7d * dto.PriceUSD * State.FeeRate * 365 * 100) /
                                 (dto.TVL * 7);
        }

        await WriteStateAsync();

        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<TradePairGrainDto>> AddOrUpdateAsync(TradePairGrainDto dto)
    {
        State = _objectMapper.Map<TradePairGrainDto, TradePairState>(dto);
        await WriteStateAsync();
        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = dto
        };
    }
}