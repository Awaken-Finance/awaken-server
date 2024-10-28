using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using Nest;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.StatInfo;

public class StatInfoAppService : ApplicationService, IStatInfoAppService
{
    private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
    private readonly INESTRepository<PoolStatInfoIndex, Guid> _poolStatInfoIndexRepository;
    private readonly INESTRepository<TokenStatInfoIndex, Guid> _tokenStatInfoIndexRepository;
    private readonly INESTRepository<TransactionHistoryIndex, Guid> _transactionHistoryIndexRepository;

    private readonly ILogger<StatInfoAppService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly StatInfoOptions _statInfoOptions;
    private readonly ITradePairAppService _tradePairAppService;
    private readonly ITokenAppService _tokenAppService;
    protected const int DataSize = 100;
    
    public StatInfoAppService(INESTRepository<StatInfoSnapshotIndex, Guid> statInfoSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ILogger<StatInfoAppService> logger,
        IOptionsSnapshot<StatInfoOptions> statInfoPeriodOptions,
        ITradePairAppService tradePairAppService,
        ITokenAppService tokenAppService,
        INESTRepository<TokenStatInfoIndex, Guid> tokenStatInfoIndexRepository,
        INESTRepository<PoolStatInfoIndex, Guid> poolStatInfoIndexRepository,
        INESTRepository<TransactionHistoryIndex, Guid> transactionHistoryIndexRepository)
    {
        _statInfoSnapshotIndexRepository = statInfoSnapshotIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _statInfoOptions = statInfoPeriodOptions.Value;
        _tradePairAppService = tradePairAppService;
        _tokenAppService = tokenAppService;
        _tokenStatInfoIndexRepository = tokenStatInfoIndexRepository;
        _poolStatInfoIndexRepository = poolStatInfoIndexRepository;
        _transactionHistoryIndexRepository = transactionHistoryIndexRepository;
    }
    
    public async Task<Tuple<long,List<StatInfoSnapshotIndex>>> GetLatestPeriodStatInfoSnapshotIndexAsync(StatType statType, long period, GetStatHistoryInput input, long timestampMax)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value((int)statType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        
        if (statType == StatType.Token)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        }
        else if (statType == StatType.Pool)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(input.PairAddress)));
        }
        
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .LessThan(timestampMax)));
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, 
            sortExp:k=>k.Timestamp, sortType:SortOrder.Descending, skip:0, limit: 1);
        return list;
    }
    
    private async Task<Tuple<long,List<StatInfoSnapshotIndex>>> GetStatInfoSnapshotIndexes(StatType statType, GetStatHistoryInput input)
    {
        var periodType = (PeriodType)input.PeriodType;
        var period = _statInfoOptions.TypePeriodMapping[periodType.ToString()];
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value((int)statType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        
        if (statType == StatType.Token)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        }
        else if (statType == StatType.Pool)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(input.PairAddress)));
        }
        
        var baseTime = DateTime.UtcNow;
        if (input.BaseTimestamp > 0)
        {
            baseTime = DateTimeHelper.FromUnixTimeMilliseconds(input.BaseTimestamp);
        }

        var timestampDateMin = baseTime.AddDays(-1);
        var timestampDateMax = baseTime;
        
        switch ((PeriodType)input.PeriodType)
        {
            case PeriodType.Week:
            {
                timestampDateMin = baseTime.AddDays(-7);
                break;
            }
            case PeriodType.Month:
            {
                timestampDateMin = baseTime.AddMonths(-1);
                break;
            }
            case PeriodType.Year:
            {
                timestampDateMin = baseTime.AddYears(-1);
                break;
            }
        }
        
        var timestampMin = DateTimeHelper.ToUnixTimeMilliseconds(timestampDateMin);
        var timestampMax = DateTimeHelper.ToUnixTimeMilliseconds(timestampDateMax);
        
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .GreaterThan(timestampMin)));
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .LessThanOrEquals(timestampMax)));
        
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);
        
        if (list.Item2.Count == 0)
        {
            return await GetLatestPeriodStatInfoSnapshotIndexAsync(statType, period, input, timestampMax);
        }
        
        return list;
    }
    
    public async Task<ListResultDto<StatInfoTvlDto>> GetTvlHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.All, input);
        return new ListResultDto<StatInfoTvlDto>
        {
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }

    public async Task<TokenTvlDto> GetTokenTvlHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Token, input);
        var tokenDto = await GetTokenDto(input.Symbol);
        return new TokenTvlDto()
        {
            Token = tokenDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }

    public async Task<PoolTvlDto> GetPoolTvlHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);

        return new PoolTvlDto()
        {
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }
    
    public async Task<TotalVolumeDto> GetVolumeHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.All, input);
        var totalVolumeInUsd = list.Item2.Select(t => t.VolumeInUsd).Sum();
        return new TotalVolumeDto
        {
            TotalVolumeInUsd = totalVolumeInUsd,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }
    
    public async Task<TokenPriceDto> GetTokenPriceHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Token, input);
        var tokenDto = await GetTokenDto(input.Symbol);
        return new TokenPriceDto()
        {
            Token = tokenDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoPriceDto>>(list.Item2)
        };
    }

    public async Task<TradePairWithTokenDto> GetTradePairDto(string chainId, string pairAddress)
    {
        var tradePair = await _tradePairAppService.GetTradePairAsync(chainId, pairAddress);
        return tradePair;
    }
    
    public async Task<TokenDto> GetTokenDto(string symbol)
    {
        return await _tokenAppService.GetAsync(new GetTokenInput()
        {
            Symbol = symbol
        });
    }
    
    public async Task<PoolPriceDto> GetPoolPriceHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);
        return new PoolPriceDto()
        {
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoPriceDto>>(list.Item2)
        };
    }
    public async Task<TokenVolumeDto> GetTokenVolumeHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Token, input);
        var tokenDto = await GetTokenDto(input.Symbol);
        var totalVolumeInUsd = list.Item2.Select(t => t.VolumeInUsd).Sum();
        return new TokenVolumeDto()
        {
            TotalVolumeInUsd = totalVolumeInUsd,
            Token = tokenDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }
    
    public async Task<PoolVolumeDto> GetPoolVolumeHistoryAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);
        var totalVolumeInUsd = list.Item2.Select(t => t.VolumeInUsd).Sum();
        return new PoolVolumeDto()
        {
            TotalVolumeInUsd = totalVolumeInUsd,
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }

    private async Task<long> GetTokenPairCountAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        
        mustQuery.Add(q => q.Bool(i => i.Should(
            s => s.Wildcard(w =>
                w.Field(f => f.TradePair.Token0.Symbol).Value($"*{symbol.ToUpper()}*")),
            s => s.Wildcard(w =>
                w.Field(f => f.TradePair.Token1.Symbol).Value($"*{symbol.ToUpper()}*")))));
        
        QueryContainer Filter(QueryContainerDescriptor<PoolStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _poolStatInfoIndexRepository.CountAsync(Filter)).Count;
    } 
    
    public async Task<ListResultDto<TokenStatInfoDto>> GetTokenStatInfoListAsync(GetTokenStatInfoListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));

        if (!string.IsNullOrEmpty(input.Symbol))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<TokenStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var list = await _tokenStatInfoIndexRepository.GetListAsync(Filter,
            limit:DataSize,
            sortExp: k => k.Tvl, 
            sortType: SortOrder.Descending);
        var tokenStatInfoDtoList = new List<TokenStatInfoDto>();
        foreach (var tokenStatInfoIndex in list.Item2)
        {
            var tokenStatInfoDto = _objectMapper.Map<TokenStatInfoIndex, TokenStatInfoDto>(tokenStatInfoIndex);
            tokenStatInfoDto.Volume24hInUsd = tokenStatInfoIndex.VolumeInUsd24h;
            tokenStatInfoDto.PairCount = await GetTokenPairCountAsync(tokenStatInfoIndex.Symbol);
            tokenStatInfoDto.Token = await GetTokenDto(tokenStatInfoIndex.Symbol);
            tokenStatInfoDtoList.Add(tokenStatInfoDto);
        }

        return new ListResultDto<TokenStatInfoDto>()
        {
            Items = tokenStatInfoDtoList
        };
    }
    
    public async Task<StatInfoSnapshotIndex> GetLatestStatInfoSnapshotIndexAsync(string pairAddress, long timestampMax)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(pairAddress)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value((int)StatType.Pool)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(86400)));
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .LessThan(timestampMax)));
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, 
            sortExp:k=>k.Timestamp, sortType:SortOrder.Descending, skip:0, limit: 1);
        return list.Item1 > 0 ? list.Item2[0] : null;
    }
    
    public async Task<List<StatInfoSnapshotIndex>> GetSnapshotIndexListAsync(string pairAddress)
    {
        var periodInDays = 7;
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(pairAddress)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value((int)StatType.Pool)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(86400)));

        var timestampDateMin = DateTime.UtcNow.AddDays(-periodInDays).Date;
        var timestampDateMax = DateTime.UtcNow.AddDays(-1).Date;
        var timestampMin = DateTimeHelper.ToUnixTimeMilliseconds(timestampDateMin);
        var timestampMax = DateTimeHelper.ToUnixTimeMilliseconds(timestampDateMax);
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .GreaterThanOrEquals(timestampMin)));
        mustQuery.Add(q => q.Range(i =>
            i.Field(f => f.Timestamp)
                .LessThanOrEquals(timestampMax)));
            
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);
        
        if (list.Item1 == periodInDays || list.Item1 == 0)
        {
            return list.Item2;
        }

        var latestSnapshotIndex = await GetLatestStatInfoSnapshotIndexAsync(pairAddress, timestampMin);
        var latestLpFeeInUsd = latestSnapshotIndex?.LpFeeInUsd ?? 0;
        var latestTvl = latestSnapshotIndex?.Tvl ?? 0;
        for (var day = 0; day < periodInDays; day++)
        {
            var snapshotTime = DateTimeHelper.ToUnixTimeMilliseconds(timestampDateMin.AddDays(day));
            var snapshotIndex = list.Item2.FirstOrDefault(t => t.Timestamp == snapshotTime);
            if (snapshotIndex == null)
            {
                list.Item2.Add(new StatInfoSnapshotIndex
                {
                    PairAddress = pairAddress,
                    LpFeeInUsd = latestLpFeeInUsd,
                    Tvl = latestTvl
                });
            }
            else
            {
                latestLpFeeInUsd = snapshotIndex.LpFeeInUsd;
                latestTvl = snapshotIndex.Tvl;
            }
        }

        return list.Item2;
    }
    
    public async Task<double> CalculateApr7dAsync(string pairAddress)
    {
        var daySnapshots = await GetSnapshotIndexListAsync(pairAddress);

        _logger.LogInformation($"CalculateApr7dAsync, pairAddress: {pairAddress}, " +
                               $"get snapshots from es begin, " +
                               $"snapshot count: {daySnapshots.Count}");
        
        if (daySnapshots.Count == 0)
        {
            return 0.0;
        }

        var sumLpFee7d = 0d;
        var sumTvl7d = 0d;
        var actualSnapshotCount = 0;
        foreach (var snapshot in daySnapshots)
        {
            ++actualSnapshotCount;
            sumTvl7d += snapshot.Tvl;
            sumLpFee7d += snapshot.LpFeeInUsd;
        }

        if (actualSnapshotCount == 0)
        {
            return 0.0;
        }

        var avgTvl = sumTvl7d / actualSnapshotCount;
        var apr7d = avgTvl > 0 ? (sumLpFee7d / avgTvl) * (360 / actualSnapshotCount) * 100 : 0;
        
        _logger.LogInformation($"CalculateApr7dAsync, pairAddress: {pairAddress}, " +
                               $"sumLpFee7d: {sumLpFee7d}," +
                               $"actualSnapshotCount: {actualSnapshotCount}," +
                               $"sumTvl7d: {sumTvl7d}," +
                               $"avgTvl: {avgTvl}, " +
                               $"apr7d: {apr7d}");

        return apr7d;
    }
    
    public async Task<ListResultDto<PoolStatInfoDto>> GetPoolStatInfoListAsync(GetPoolStatInfoListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolStatInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));

        if (!string.IsNullOrEmpty(input.PairAddress))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePair.Address).Value(input.PairAddress)));
        }
        if (!string.IsNullOrEmpty(input.Symbol))
        {
            mustQuery.Add(q => q.Bool(i => i.Should(
                s => s.Wildcard(w =>
                    w.Field(f => f.TradePair.Token0.Symbol).Value($"*{input.Symbol.ToUpper()}*")),
                s => s.Wildcard(w =>
                    w.Field(f => f.TradePair.Token1.Symbol).Value($"*{input.Symbol.ToUpper()}*")))));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<PoolStatInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _poolStatInfoIndexRepository.GetListAsync(Filter,
            limit:DataSize,
            sortExp: k => k.Tvl, 
            sortType: SortOrder.Descending);
        var poolStatInfoDtoList = new List<PoolStatInfoDto>();
        foreach (var poolStatInfoIndex in list.Item2)
        {
            var poolStatInfoDto = _objectMapper.Map<PoolStatInfoIndex, PoolStatInfoDto>(poolStatInfoIndex);
            poolStatInfoDto.Volume24hInUsd = poolStatInfoIndex.VolumeInUsd24h;
            poolStatInfoDto.Volume7dInUsd = poolStatInfoIndex.VolumeInUsd7d;
            poolStatInfoDto.ValueLocked0 = poolStatInfoIndex.ValueLocked0;
            poolStatInfoDto.ValueLocked1 = poolStatInfoIndex.ValueLocked1;
            poolStatInfoDto.Apr7d = await CalculateApr7dAsync(poolStatInfoIndex.PairAddress);
            poolStatInfoDtoList.Add(poolStatInfoDto);
        }

        return new ListResultDto<PoolStatInfoDto>()
        {
            Items = poolStatInfoDtoList
        };
    }
    
    public async Task<ListResultDto<TransactionHistoryDto>> GetTransactionStatInfoListAsync(GetTransactionStatInfoListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TransactionHistoryIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.DataVersion)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionType).Value(input.TransactionType)));
        
        QueryContainer Filter(QueryContainerDescriptor<TransactionHistoryIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _transactionHistoryIndexRepository.GetListAsync(Filter,
            limit:DataSize,
            sortExp: k => k.Timestamp,
            sortType: SortOrder.Ascending);
        var transactionHistoryDtoList = new List<TransactionHistoryDto>();
        foreach (var transactionHistoryIndex in list.Item2)
        {
            var transactionHistoryDto = _objectMapper.Map<TransactionHistoryIndex, TransactionHistoryDto>(transactionHistoryIndex);
            transactionHistoryDto.TransactionId = transactionHistoryIndex.TransactionHash;
            transactionHistoryDto.TradeType = (int)transactionHistoryIndex.Side;
            transactionHistoryDtoList.Add(transactionHistoryDto);
        }

        return new ListResultDto<TransactionHistoryDto>()
        {
            Items = transactionHistoryDtoList
        };
    }
}