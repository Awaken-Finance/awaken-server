using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        ITokenAppService tokenAppService)
    {
        _statInfoSnapshotIndexRepository = statInfoSnapshotIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _statInfoOptions = statInfoPeriodOptions.Value;
        _tradePairAppService = tradePairAppService;
        _tokenAppService = tokenAppService;
    }

    private async Task<Tuple<long,List<StatInfoSnapshotIndex>>> GetStatInfoSnapshotIndexes(StatType statType, GetStatHistoryInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<StatInfoSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.StatType).Value((int)statType)));
        if (statType == StatType.Token)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        }
        else if (statType == StatType.Pool)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PairAddress).Value(input.PairAddress)));

        }

        // todo get time range by period type
        
        var periodType = (PeriodType)input.PeriodType;
        var period = _statInfoOptions.TypePeriodMapping[periodType.ToString()];
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(period)));
        
        QueryContainer Filter(QueryContainerDescriptor<StatInfoSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _statInfoSnapshotIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);
        return list;
    }
    
    public async Task<ListResultDto<StatInfoTvlDto>> GetTvlListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.All, input);
        return new ListResultDto<StatInfoTvlDto>
        {
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }

    public async Task<TokenTvlDto> GetTokenTvlListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Token, input);
        var tokenDto = await GetTokenDto(input.Symbol);
        return new TokenTvlDto()
        {
            Token = tokenDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }

    public async Task<PoolTvlDto> GetPoolTvlListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);

        return new PoolTvlDto()
        {
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoTvlDto>>(list.Item2)
        };
    }
    
    public async Task<ListResultDto<StatInfoVolumeDto>> GetVolumeListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.All, input);
        return new ListResultDto<StatInfoVolumeDto>
        {
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }
    
    public async Task<TokenPriceDto> GetTokenPriceListAsync(GetStatHistoryInput input)
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
    
    public async Task<PoolPriceDto> GetPoolPriceListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);
        return new PoolPriceDto()
        {
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoPriceDto>>(list.Item2)
        };
    }
    public async Task<TokenVolumeDto> GetTokenVolumeListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Token, input);
        var tokenDto = await GetTokenDto(input.Symbol);
        return new TokenVolumeDto()
        {
            Token = tokenDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }
    
    public async Task<PoolVolumeDto> GetPoolVolumeListAsync(GetStatHistoryInput input)
    {
        var list = await GetStatInfoSnapshotIndexes(StatType.Pool, input);
        var tradePairDto = await GetTradePairDto(input.ChainId, input.PairAddress);

        return new PoolVolumeDto()
        {
            TradePair = tradePairDto,
            Items = _objectMapper.Map<List<StatInfoSnapshotIndex>, List<StatInfoVolumeDto>>(list.Item2)
        };
    }
    
    
    public async Task<ListResultDto<TokenStatInfoDto>> GetTokenStatInfoListAsync(GetTokenStatInfoListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TokenStatInfoIndex>, QueryContainer>>();
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
            tokenStatInfoDto.PairCount = 0;//todo
            tokenStatInfoDto.Token = await GetTokenDto(tokenStatInfoIndex.Symbol);
            tokenStatInfoDtoList.Add(tokenStatInfoDto);
        }

        return new ListResultDto<TokenStatInfoDto>()
        {
            Items = tokenStatInfoDtoList
        };
    }
    
    public async Task<ListResultDto<PoolStatInfoDto>> GetPoolStatInfoListAsync(GetPoolStatInfoListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PoolStatInfoIndex>, QueryContainer>>();
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
            poolStatInfoDto.ValueLocked0 = poolStatInfoIndex.ReserveA;// todo decimal
            poolStatInfoDto.ValueLocked1 = poolStatInfoIndex.ReserveB;// todo decimal
            poolStatInfoDto.Apr7d = 0;// todo
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
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionType).Value(input.TransactionType)));
        
        QueryContainer Filter(QueryContainerDescriptor<TransactionHistoryIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _transactionHistoryIndexRepository.GetListAsync(Filter,
            limit:DataSize,
            sortExp: k => k.ValueInUsd, 
            sortType: SortOrder.Descending);
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