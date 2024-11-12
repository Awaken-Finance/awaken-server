using System.Reflection;
using AElf.OpenTelemetry.ExecutionTime;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;
using Newtonsoft.Json;
using Orleans.Core;
using Serilog;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price.TradePair;

[KeepAlive]
public class TradePairGrain : Grain<TradePairState>, ITradePairGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger _logger;
    private readonly IClusterClient _clusterClient;
    private SortedDictionary<DateTime, string> _latestMarketDataSnapshots;

    public TradePairGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _logger = Log.ForContext<TradePairGrain>();
        _clusterClient = clusterClient;
        _latestMarketDataSnapshots = new SortedDictionary<DateTime, string>(
            Comparer<DateTime>.Create((datetime1, datetime2) => { return datetime2.CompareTo(datetime1); })
        );
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await LoadLatestSnapshots();
        _logger.Information($"TradePairGrain OnActivateAsync, pair address: {State.Address}");
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task LoadLatestSnapshots()
    {
        foreach (var snapshotId in State.MarketDataSnapshotGrainIds)
        {
            var snapshotGrain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshotId);
            var snapshotDataResult = await snapshotGrain.GetAsync();

            if (snapshotDataResult.Success && snapshotDataResult.Data != null)
            {
                if (_latestMarketDataSnapshots.Count >= 7 * 24)
                {
                    var lastKey = _latestMarketDataSnapshots.Last().Key;
                    _logger.Information($"pop the least recently used snapshot: {_latestMarketDataSnapshots.Last().Value}, latest snapshot: {_latestMarketDataSnapshots.First().Value}");
                    bool removed = _latestMarketDataSnapshots.Remove(lastKey);
                    if (!removed)
                    {
                        _logger.Error($"pop the least recently used snapshot failed.");
                    }
                }
                _latestMarketDataSnapshots.Add(snapshotDataResult.Data.Timestamp,
                    snapshotGrain.GetPrimaryKeyString());
            }
        }
    }

    public async Task<GrainResultDto<TradePairGrainDto>> GetAsync()
    {
        if (State.Id == Guid.Empty || State.IsDeleted)
        {
            _logger.Error($"TradePairGrain, GetAsync error, etag: State.Id: {State.Id}, IsDeleted: {State.IsDeleted}, grain id: {this.GetGrainId()}, PrimaryKeyString: {this.GetPrimaryKeyString()}");
            return new GrainResultDto<TradePairGrainDto>
            {
                Success = false
            };
        }

        _logger.Information($"TradePairGrain, GetAsync find result, State.Id: {State.Id}, IsDeleted: {State.IsDeleted}, grain id: {this.GetGrainId()}, address: {State.Address}, feeRate: {State.FeeRate}");
        return new GrainResultDto<TradePairGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TradePairState, TradePairGrainDto>(State)
        };
    }

    public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestBeforeNeqSnapshotAsync(DateTime maxTime)
    {
        foreach (var snapshot in _latestMarketDataSnapshots)
        {
            if (maxTime > snapshot.Key)
            {
                var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
                var result = await grain.GetAsync();
                if (result.Success)
                {
                    return result.Data;
                }
            }
        }

        return null;
    }

    public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestBeforeSnapshotAsync(DateTime maxTime)
    {
        foreach (var snapshot in _latestMarketDataSnapshots)
        {
            if (maxTime >= snapshot.Key)
            {
                var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
                var result = await grain.GetAsync();
                if (result.Success)
                {
                    return result.Data;
                }
            }
        }

        return null;
    }


    public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestSnapshotAsync()
    {
        if (_latestMarketDataSnapshots.IsNullOrEmpty())
        {
            return null;
        }

        foreach (var snapshot in _latestMarketDataSnapshots)
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
        if (_latestMarketDataSnapshots.IsNullOrEmpty())
        {
            return null;
        }

        return _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(_latestMarketDataSnapshots
            .FirstOrDefault().Value);
    }

    public async Task<List<TradePairMarketDataSnapshotGrainDto>> GetPrevious7DaysSnapshotsDtoAsync()
    {
        DateTime now = DateTime.Now;
        DateTime pastWeek = now.AddDays(-7);
        var sortedSnapshots = new List<TradePairMarketDataSnapshotGrainDto>();
        foreach (var snapshot in _latestMarketDataSnapshots)
        {
            var grain = _clusterClient.GetGrain<ITradePairMarketDataSnapshotGrain>(snapshot.Value);
            var result = await grain.GetAsync();
            if (result.Success && result.Data.Timestamp >= pastWeek)
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
            TradeCount = dto.IsRevert ? -1 : 1,
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

        _logger.Information($"update total supply, " +
                               $"pair id: {State.Id}, " +
                               $"txn hash: {dto.TransactionHash}, " +
                               $"liquidity type: {dto.Type}, " +
                               $"is revert: {dto.IsRevert}, " +
                               $"lp amount: {lpAmount.ToNormalizeString()}");

        var updateResult = await AddOrUpdateSnapshotAsync(
            new TradePairMarketDataSnapshotGrainDto
            {
                Id = Guid.NewGuid(),
                ChainId = dto.ChainId,
                TradePairId = State.Id,
                Timestamp = GetSnapshotTime(dto.Timestamp),
                TotalSupply = dto.TotalSupply,
                LpAmount = lpAmount.ToNormalizeString()
            });

        // nie:The current snapshot is not up-to-date. The latest snapshot needs to update TotalSupply 
        var latestSnapshot = await GetLatestSnapshotAsync();
        if (latestSnapshot != null && updateResult.Data.SnapshotDto.Timestamp < latestSnapshot.Timestamp)
        {
            var latestGrain = await GetLatestSnapshotGrainAsync();
            var latestResult = await latestGrain.AccumulateTotalSupplyAsync(lpAmount.ToNormalizeString());
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

        _logger.Information(
            "SyncEvent, input chainId: {chainId}, isReversed: {isReversed}, token0Amount: {token0Amount}, " +
            "token1Amount: {token1Amount}, tradePairId: {tradePairId}, timestamp: {timestamp}, blockHeight: {blockHeight}",
            dto.ChainId,
            isReversed, token0Amount, token1Amount, State.Id, dto.Timestamp, dto.BlockHeight);

        var timestamp = DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp);
        var price = double.Parse(token1Amount) / double.Parse(token0Amount);
        
        var priceUSD0 = dto.Token0PriceInUsd;
        var priceUSD1 = dto.Token1PriceInUsd;

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

    public async Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>>
        AddOrUpdateSnapshotAsync(TradePairMarketDataSnapshotGrainDto snapshotDto)
    {
        if (State.Id == Guid.Empty || State.Token0 == null || State.Token1 == null)
        {
            _logger.Error($"add snapshot to an error trade pair, id: {snapshotDto.TradePairId}, " +
                             $"timestamp: {snapshotDto.Timestamp}");
            return new GrainResultDto<TradePairMarketDataSnapshotUpdateResult>
            {
                Success = false
            };
        }

        snapshotDto.Timestamp = GetSnapshotTime(snapshotDto.Timestamp);

        _logger.Information(
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
            if (_latestMarketDataSnapshots.Count >= 7 * 24)
            {
                var lastKey = _latestMarketDataSnapshots.Last().Key;
                bool removed = _latestMarketDataSnapshots.Remove(lastKey);
                if (!removed)
                {
                    _logger.Error("previous 7 days market data snapshots remove failed");
                }
            }
            
            _latestMarketDataSnapshots.Add(snapshotDto.Timestamp,
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

    public async Task<GrainResultDto<TradePairGrainDto>> UpdateAsync(DateTime timestamp,
        int userTradeAddressCount,
        string totalSupply, 
        double token0PriceInUsd, 
        double token1PriceInUsd)
    {
        _logger.Debug($"Scheduled trade pair update begin, id: {State.Id}, " +
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
            _logger.Information($"Scheduled trade pair update, " +
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
            _logger.Information($"scheduled trade pair update, get last day snapshot from lastDaySnapshot, time: {snapshot.Timestamp}, lastDayTvl: {lastDayTvl}, lastDayPriceUSD: {lastDayPriceUSD}");

        }
        else
        {
            var sortDaySnapshot = daySnapshot.OrderBy(s => s.Timestamp).ToList();
            if (sortDaySnapshot.Count > 0)
            {
                var snapshot = sortDaySnapshot.First();
                lastDayTvl = snapshot.TVL;
                lastDayPriceUSD = snapshot.PriceUSD;
                _logger.Information($"scheduled trade pair update, get last day snapshot from daySnapshot, time: {snapshot.Timestamp}, lastDayTvl: {lastDayTvl}, lastDayPriceUSD: {lastDayPriceUSD}");

            }
        }

        _logger.Information($"Scheduled trade pair update, " +
                               $"lastDaySnapshot count: {lastDaySnapshot.Count}, " +
                               $"lastDayVolume24h: {lastDayVolume24h}, " +
                               $"lastDayTvl: {lastDayTvl}, " +
                               $"lastDayPriceUSD: {lastDayPriceUSD}");

        var priceUSD0 = token0PriceInUsd;
        var priceUSD1 = token1PriceInUsd;

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

        _logger.Debug($"Scheduled trade pair update end, id: {State.Id}, " +
                         $"timestamp: {timestamp}, " +
                         $"after update, trade pair: {JsonConvert.SerializeObject(State)}");

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
        _logger.Debug($"update pair from snapshot begin, id: {State.Id}, " +
                               $"snapshot: {JsonConvert.SerializeObject(dto)}, " +
                               $"current trade pair: {JsonConvert.SerializeObject(State)}");
        
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

        var lastDaySnapshot = previous7DaysSnapshotDtos
            .Where(s => s.Timestamp >= dto.Timestamp.AddDays(-2) && s.Timestamp < dto.Timestamp.AddDays(-1))
            .OrderByDescending(s => s.Timestamp).ToList();

        var lastDayVolume24h = lastDaySnapshot.Sum(snapshot => snapshot.Volume);
        var lastDayTvl = 0d;
        var lastDayPriceUSD = 0d;

        if (lastDaySnapshot.Count > 0)
        {
            var snapshot = lastDaySnapshot.First();
            lastDayTvl = snapshot.TVL;
            lastDayPriceUSD = snapshot.PriceUSD;
            _logger.Information(
                $"get last day snapshot from lastDaySnapshot, time: {snapshot.Timestamp}, lastDayTvl: {lastDayTvl}, lastDayPriceUSD: {lastDayPriceUSD}");
        }
        else
        {
            var latestBeforeThisSnapshotDto = await GetLatestBeforeNeqSnapshotAsync(dto.Timestamp);
            if (latestBeforeThisSnapshotDto != null)
            {
                lastDayTvl = latestBeforeThisSnapshotDto.TVL;
                lastDayPriceUSD = latestBeforeThisSnapshotDto.PriceUSD;
                _logger.Information(
                    $"get last day snapshot from daySnapshot, time: {latestBeforeThisSnapshotDto.Timestamp}, lastDayTvl: {lastDayTvl}, lastDayPriceUSD: {lastDayPriceUSD}");
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

        _logger.Debug($"update pair from snapshot end, id: {State.Id}, " +
                         $"dto timestamp: {dto.Timestamp}, " +
                         $"current trade pair: {JsonConvert.SerializeObject(State)}");
        
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