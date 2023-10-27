using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Price.UserTradeSummary;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Provider;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade
{
    [RemoteService(IsEnabled = false)]
    public class TradeRecordAppService : ApplicationService, ITradeRecordAppService
    {
        private readonly INESTRepository<Index.TradeRecord, Guid> _tradeRecordIndexRepository;
        private readonly INESTRepository<Index.UserTradeSummary, Guid> _userTradeSummaryIndexRepository;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly ILocalEventBus _localEventBus;
        private readonly ILogger<TradeRecordAppService> _logger;
        private readonly TradeRecordOptions _tradeRecordOptions;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IDistributedCache<BlockHeightSetDto> _blockHeightSetCache;
        private readonly IDistributedCache<TransactionHashSetDto> _transactionHashSetCache;
        private readonly IDistributedCache<TransactionHashDto> _transactionHashCache;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly IBus _bus;

        private const string ASC = "asc";
        private const string ASCEND = "ascend";
        private const string TIMESTAMP = "timestamp";
        private const string TRADEPAIR = "tradepair";
        private const string SIDE = "side";
        private const string TOTALPRICEINUSD = "totalpriceinusd";

        public TradeRecordAppService(INESTRepository<Index.TradeRecord, Guid> tradeRecordIndexRepository,
            INESTRepository<Index.UserTradeSummary, Guid> userTradeSummaryIndexRepository,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository,
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            ILocalEventBus localEventBus,
            ILogger<TradeRecordAppService> logger,
            IOptionsSnapshot<TradeRecordOptions> tradeRecordOptions,
            IDistributedEventBus distributedEventBus,
            IDistributedCache<BlockHeightSetDto> blockHeightSetCache,
            IDistributedCache<TransactionHashSetDto> transactionHashSetCache,
            IDistributedCache<TransactionHashDto> transactionHashCache,
            IGraphQLProvider graphQlProvider,
            IBus bus)
        {
            _tradeRecordIndexRepository = tradeRecordIndexRepository;
            _userTradeSummaryIndexRepository = userTradeSummaryIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _localEventBus = localEventBus;
            _logger = logger;
            _tradeRecordOptions = tradeRecordOptions.Value;
            _distributedEventBus = distributedEventBus;
            _blockHeightSetCache = blockHeightSetCache;
            _transactionHashSetCache = transactionHashSetCache;
            _transactionHashCache = transactionHashCache;
            _graphQlProvider = graphQlProvider;
            _bus = bus;
        }

        public async Task<PagedResultDto<TradeRecordIndexDto>> GetListAsync(GetTradeRecordsInput input)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            if (input.TradePairId != null)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePair.Id).Value(input.TradePairId)));
            }

            if (!string.IsNullOrWhiteSpace(input.TransactionHash))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionHash).Value(input.TransactionHash)));
            }

            if (!string.IsNullOrWhiteSpace(input.Address))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
            }

            if (!string.IsNullOrWhiteSpace(input.TokenSymbol))
            {
                mustQuery.Add(q => q.Bool(i => i.Should(
                    s => s.Wildcard(w => w.Field(f => f.TradePair.Token0.Symbol).Value($"*{input.TokenSymbol.ToUpper()}*")),
                    s => s.Wildcard(w => w.Field(f => f.TradePair.Token1.Symbol).Value($"*{input.TokenSymbol.ToUpper()}*")))));
            }

            if (input.TimestampMin != 0)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.Timestamp)
                        .GreaterThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin))));
            }

            if (input.TimestampMax != 0)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.Timestamp)
                        .LessThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax))));
            }

            if (input.FeeRate != 0)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePair.FeeRate).Value(input.FeeRate)));
            }

            if (input.Side.HasValue)
            {
                if (!(input.Side.Value == 0 || input.Side.Value == 1))
                {
                    return new PagedResultDto<TradeRecordIndexDto>
                    {
                        Items = null,
                        TotalCount = 0
                    };
                }

                var side = input.Side.Value == 0 ? TradeSide.Buy : TradeSide.Sell;
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Side).Value(side)));
            }

            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));

            List<Index.TradeRecord> item2;
            if (!string.IsNullOrEmpty(input.Sorting))
            {
                var sorting = GetSorting(input.Sorting);
                var list = await _tradeRecordIndexRepository.GetSortListAsync(Filter,
                    sortFunc: sorting,
                    limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize : input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
                    skip: input.SkipCount);
                item2 = list.Item2;
            }
            else
            {
                var list = await _tradeRecordIndexRepository.GetSortListAsync(Filter,
                    sortFunc: s => s.Descending(t => t.Timestamp),
                    limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize : input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
                    skip: input.SkipCount);
                item2 = list.Item2;
            }

            var totalCount = await _tradeRecordIndexRepository.CountAsync(Filter);

            return new PagedResultDto<TradeRecordIndexDto>
            {
                Items = ObjectMapper.Map<List<Index.TradeRecord>, List<TradeRecordIndexDto>>(item2),
                TotalCount = totalCount.Count
            };
        }

        public async Task CreateAsync(TradeRecordCreateDto input)
        {
            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(input);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();
            var tradeRecordGrain = _clusterClient.GetGrain<ITradeRecordGrain>(tradeRecord.Id);
            await tradeRecordGrain.InsertAsync(ObjectMapper.Map<TradeRecord, TradeRecordGrainDto>(tradeRecord));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradeRecordEto>(
                ObjectMapper.Map<TradeRecord, TradeRecordEto>(tradeRecord)
            ));

            var userTradeSummaryGrain =
                _clusterClient.GetGrain<IUserTradeSummaryGrain>(
                    GrainIdHelper.GenerateGrainId(input.ChainId, input.TradePairId, input.Address));
            var userTradeSummaryResult = await userTradeSummaryGrain.GetAsync();
            if (!userTradeSummaryResult.Success)
            {
                var userTradeSummary = new UserTradeSummaryGrainDto
                {
                    Id = Guid.NewGuid(),
                    ChainId = input.ChainId,
                    TradePairId = input.TradePairId,
                    Address = input.Address,
                    LatestTradeTime = tradeRecord.Timestamp
                };

                await userTradeSummaryGrain.AddOrUpdateAsync(userTradeSummary);
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<UserTradeSummaryGrainDto, UserTradeSummaryEto>(userTradeSummary)
                );
            }
            else
            {
                userTradeSummaryResult.Data.LatestTradeTime = tradeRecord.Timestamp;
                await userTradeSummaryGrain.AddOrUpdateAsync(userTradeSummaryResult.Data);
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<UserTradeSummaryGrainDto, UserTradeSummaryEto>(userTradeSummaryResult.Data)
                );
            }

            await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));
        }

        public async Task<bool> CreateAsync(SwapRecordDto dto)
        {
            var grain = _clusterClient.GetGrain<ILiquiditySyncGrain>(GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (await grain.ExistTransactionHashAsync(dto.TransactionHash))
            {
                _logger.LogInformation("swap event transactionHash existed: {transactionHash}", dto.TransactionHash);
                return false;
            }
            
            var pair = await GetAsync(dto.ChainId, dto.PairAddress);
            if (pair == null)
            {
                _logger.LogInformation("swap can not find trade pair: {chainId}, {pairAddress}", dto.ChainId, dto.PairAddress);
                return false;
            }

            var isSell = pair.Token0.Symbol == dto.SymbolIn;
            var record = new TradeRecordCreateDto
            {
                ChainId = dto.ChainId,
                TradePairId = pair.Id,
                Address = dto.Sender,
                TransactionHash = dto.TransactionHash,
                Timestamp = dto.Timestamp,
                Side = isSell ? TradeSide.Sell : TradeSide.Buy,
                Token0Amount = isSell ? dto.AmountIn.ToDecimalsString(pair.Token0.Decimals) : dto.AmountOut.ToDecimalsString(pair.Token0.Decimals),
                Token1Amount = isSell ? dto.AmountOut.ToDecimalsString(pair.Token1.Decimals) : dto.AmountIn.ToDecimalsString(pair.Token1.Decimals),
                TotalFee = dto.TotalFee / Math.Pow(10, pair.Token0.Decimals),
                Channel = dto.Channel,
                Sender = dto.Sender,
                BlockHeight = dto.BlockHeight
            };

            _logger.LogInformation("SwapEvent, input chainId: {chainId}, tradePairId: {tradePairId}, address: {address}, " +
                "transactionHash: {transactionHash}, timestamp: {timestamp}, side: {side}, channel: {channel}, token0Amount: {token0Amount}, token1Amount: {token1Amount}, " +
                "blockHeight: {blockHeight}, totalFee: {totalFee}", dto.ChainId, pair.Id, dto.Sender, dto.TransactionHash, dto.Timestamp, 
                record.Side, dto.Channel, record.Token0Amount, record.Token1Amount, dto.BlockHeight, dto.TotalFee);
            await CreateAsync(record);
            await grain.AddTransactionHashAsync(dto.TransactionHash);
            await CreateCacheAsync(pair.Id, dto);
            return true;
        }

        public async Task CreateCacheAsync(Guid tradePairId, SwapRecordDto dto)
        {
            var startIndex = 0;
            while (startIndex >= 0)
            {
                var key = $"{dto.ChainId}:{TradeRecordOptions.BlockHeightSetPrefix}:{startIndex}";
                var cache = await _blockHeightSetCache.GetOrAddAsync(key, async () => new BlockHeightSetDto());
                if (cache.BlockHeight.Contains(dto.BlockHeight))
                {
                    await _blockHeightSetCache.RefreshAsync(key);
                    await CreateTransactionHashCacheAsync(tradePairId, dto);
                    break;
                }
                if (cache.BlockHeight.Count < _tradeRecordOptions.BlockHeightLimit)
                {
                    cache.BlockHeight.Add(dto.BlockHeight);
                    await _blockHeightSetCache.SetAsync(key, cache);
                    await CreateTransactionHashCacheAsync(tradePairId, dto);
                    break;
                }
                if (cache.NextNode > 0)
                {
                    startIndex = cache.NextNode;
                    continue;
                }
                cache.NextNode = startIndex + 1;
                await _blockHeightSetCache.SetAsync(key, cache);
                startIndex = cache.NextNode;
            }
        }

        public async Task RevertAsync(string chainId)
        {
            var confirmedHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainId);
            var txnHashList = new List<TransactionHashDto>();
            var minBlockHeight = 0L;
            var startIndex = 0;
            try
            {
                while (startIndex >= 0)
                {
                    var key = $"{chainId}:{TradeRecordOptions.BlockHeightSetPrefix}:{startIndex}";
                    var dto = await _blockHeightSetCache.GetAsync(key);
                    startIndex += 1;
                    if (dto == null) break;
                    var heightCount = dto.BlockHeight.Count;
                    foreach (var blockHeight in dto.BlockHeight)
                    {
                        _logger.LogInformation("query cache when revert: {chainId}, {blockHeight}, {confirmedHeight}", chainId, blockHeight, confirmedHeight);
                        if (blockHeight > confirmedHeight) continue;
                        var heightKey = $"{chainId}:{TradeRecordOptions.TransactionHashSetPrefix}:{blockHeight}";
                        var txnSetDto = await _transactionHashSetCache.GetAsync(heightKey);
                        if (txnSetDto == null) continue;
                        foreach (var transactionHash in txnSetDto.TransactionHash)
                        {
                            var txnKey = $"{chainId}:{TradeRecordOptions.TransactionHashPrefix}:{transactionHash}";
                            var txnDto = await _transactionHashCache.GetAsync(txnKey);
                            if (txnDto == null)
                            {
                                txnSetDto.TransactionHash.Remove(transactionHash);
                                continue;
                            }
                            txnDto.Retry += 1;
                            _logger.LogInformation("current retry when revert: {chainId}, {transactionHash}, {retry}", chainId, transactionHash, txnDto.Retry);
                            if (txnDto.Retry > _tradeRecordOptions.RetryLimit) continue;

                            await _transactionHashCache.SetAsync(txnKey, txnDto, new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration =
                                    DateTimeOffset.UtcNow.AddSeconds(_tradeRecordOptions.TransactionHashExpirationTime)
                            });
                            minBlockHeight = Math.Min(minBlockHeight == 0 ? txnDto.BlockHeight : minBlockHeight, txnDto.BlockHeight);
                            txnHashList.Add(txnDto);
                        }
                        if (txnSetDto.TransactionHash.Count == 0)
                        {
                            await _transactionHashSetCache.RemoveAsync(heightKey);
                            dto.BlockHeight.Remove(blockHeight);
                        }
                    }
                    if (heightCount > dto.BlockHeight.Count)
                    {
                        await _blockHeightSetCache.SetAsync(key, dto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "query cache fail when revert.");
                await RequestEsAsync(chainId, confirmedHeight);
                return;
            }
            
            _logger.LogInformation("cache txnHash when revert: {chainId}, {count}", chainId, txnHashList.Count);
            if (txnHashList.Count == 0) return;
            var tradeRecordList = await QueryAsync(chainId, confirmedHeight);
            await RequestGraphQlAsync(chainId, minBlockHeight, confirmedHeight, txnHashList, tradeRecordList);
        }

        public async Task<int> GetUserTradeAddressCountAsync(string chainId, Guid tradePairId,
            DateTime? minDateTime = null, DateTime? maxDateTime = null)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.UserTradeSummary>, QueryContainer>>();
            if (!string.IsNullOrWhiteSpace(chainId))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            }

            if (tradePairId != Guid.Empty)
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
            }

            if (minDateTime.HasValue)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.LatestTradeTime)
                        .GreaterThanOrEquals(minDateTime.Value.AddDays(-1))));
            }

            if (maxDateTime.HasValue)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.LatestTradeTime)
                        .LessThanOrEquals(maxDateTime.Value)));
            }

            QueryContainer Filter(QueryContainerDescriptor<Index.UserTradeSummary> f) => f.Bool(b => b.Must(mustQuery));

            var result = await _userTradeSummaryIndexRepository.CountAsync(Filter);

            return int.TryParse(result.Count.ToString(), out int count) ? count : 0;
        }

        public async Task<Index.TradePair> GetAsync(string chainName, string address)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));
            return await _tradePairIndexRepository.GetAsync(Filter);
        }

        private async Task<List<Index.TradeRecord>> GetListAsync(string chainId, long blockHeight, int skipCount, int maxResultCount)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsConfirmed).Value(false)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(blockHeight)));

            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradeRecordIndexRepository.GetListAsync(Filter, limit: maxResultCount, skip: skipCount, sortExp: m => m.BlockHeight);
            return list.Item2;
        }

        private static Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>> GetSorting(string sorting)
        {
            var result =
                new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                    s.Descending(t => t.Timestamp));
            if (string.IsNullOrWhiteSpace(sorting)) return result;
            
            var sortingArray = sorting.Trim().ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
            switch (sortingArray.Length)
            {
                case 1:
                    switch (sortingArray[0])
                    {
                        case TIMESTAMP:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                s.Ascending(t => t.Timestamp));
                            break;
                        case TRADEPAIR:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                s.Ascending(t => t.TradePair.Token0.Symbol)
                                    .Ascending(t => t.TradePair.Token1.Symbol));
                            break;
                        case SIDE:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                s.Ascending(t => t.Side));
                            break;
                        case TOTALPRICEINUSD:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                s.Ascending(t => t.TotalPriceInUsd));
                            break;
                    }
                    break;
                case 2:
                    switch (sortingArray[0])
                    {
                        case TIMESTAMP:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                sortingArray[1] == ASC || sortingArray[1] == ASCEND
                                    ? s.Ascending(t => t.Timestamp)
                                    : s.Descending(t => t.Timestamp));
                            break;
                        case TRADEPAIR:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(
                                s => sortingArray[1] == ASC || sortingArray[1] == ASCEND
                                    ? s.Ascending(t => t.TradePair.Token0.Symbol)
                                        .Ascending(t => t.TradePair.Token1.Symbol)
                                    : s.Descending(t => t.TradePair.Token0.Symbol)
                                        .Descending(t => t.TradePair.Token1.Symbol));
                            break;
                        case SIDE:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                sortingArray[1] == ASC || sortingArray[1] == ASCEND
                                    ? s.Ascending(t => t.Side)
                                    : s.Descending(t => t.Side));
                            break;
                        case TOTALPRICEINUSD:
                            result = new Func<SortDescriptor<Index.TradeRecord>, IPromise<IList<ISort>>>(s =>
                                sortingArray[1] == ASC || sortingArray[1] == ASCEND
                                    ? s.Ascending(t => t.TotalPriceInUsd)
                                    : s.Descending(t => t.TotalPriceInUsd));
                            break;
                    }
                    break;
            }
            
            return result;
        }

        private async Task CreateTransactionHashCacheAsync(Guid tradePairId, SwapRecordDto dto)
        {
            var heightKey = $"{dto.ChainId}:{TradeRecordOptions.TransactionHashSetPrefix}:{dto.BlockHeight}";
            var txnSetDto = await _transactionHashSetCache.GetOrAddAsync(heightKey, async () => new TransactionHashSetDto());
            if(txnSetDto.TransactionHash.Contains(dto.TransactionHash))
            {
                await _transactionHashSetCache.RefreshAsync(heightKey);
            }
            else
            {
                txnSetDto.TransactionHash.Add(dto.TransactionHash);
                await _transactionHashSetCache.SetAsync(heightKey, txnSetDto);
            }
                    
            var txnKey = $"{dto.ChainId}:{TradeRecordOptions.TransactionHashPrefix}:{dto.TransactionHash}";
            var txnDto = new TransactionHashDto()
            {
                Address = dto.Sender,
                TradePairId = tradePairId,
                BlockHeight = dto.BlockHeight,
                TransactionHash = dto.TransactionHash
            };
            await _transactionHashCache.SetAsync(txnKey, txnDto, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_tradeRecordOptions.TransactionHashExpirationTime)
            });
        }
        
        private async Task<List<Index.TradeRecord>> QueryAsync(string chainId, long confirmedHeight)
        {
            var tradeRecordList = new List<Index.TradeRecord>();
            var skipCount = 0;
            var totalCount = 1;
            while (totalCount > 0)
            {
                var recordList = await GetListAsync(chainId, confirmedHeight, skipCount, _tradeRecordOptions.QueryOnceLimit);
                if (recordList.Count == 0) break;
                tradeRecordList.AddRange(recordList);
                skipCount += _tradeRecordOptions.QueryOnceLimit;
                totalCount = recordList.Count;
            }
            return tradeRecordList;
        }

        private async Task RequestEsAsync(string chainId, long confirmedHeight)
        {
            var tradeRecordList = await QueryAsync(chainId, confirmedHeight);
            _logger.LogInformation("persistence txnHash when revert: {chainId}, {count}", chainId, tradeRecordList.Count);
            if (tradeRecordList.Count == 0) return;
            
            var minBlockHeight = tradeRecordList[0].BlockHeight;
            var txnHashList = new List<TransactionHashDto>();
            txnHashList.AddRange(tradeRecordList.Select(t => new TransactionHashDto()
            {
                Address = t.Address,
                TradePairId = t.TradePair.Id,
                BlockHeight = t.BlockHeight,
                TransactionHash = t.TransactionHash,
                Retry = (DateTime.UtcNow - t.Timestamp).Milliseconds > _tradeRecordOptions.RevertTimePeriod ? _tradeRecordOptions.RetryLimit : 1
            }));

            await RequestGraphQlAsync(chainId, minBlockHeight, confirmedHeight, txnHashList, tradeRecordList);
        }

        private async Task RequestGraphQlAsync(string chainId, long minBlockHeight, long confirmedHeight, List<TransactionHashDto> txnHashList, List<Index.TradeRecord> tradeRecordList)
        {
            var revertTxnHashList = new List<TransactionHashDto>();
            var txnHashs = new List<string>();
            while (minBlockHeight <= confirmedHeight)
            {
                var endBlockHeight = minBlockHeight + _tradeRecordOptions.QueryOnceLimit > confirmedHeight
                    ? confirmedHeight
                    : minBlockHeight + _tradeRecordOptions.QueryOnceLimit;
                var dtoList = await _graphQlProvider.GetSwapRecordsAsync(chainId, minBlockHeight, endBlockHeight);
                var records = dtoList.Select(t => t.TransactionHash).ToList();
                txnHashs.AddRange(records);
                
                minBlockHeight = endBlockHeight;
                if (minBlockHeight == confirmedHeight) break;
            }
            
            _logger.LogInformation("query list when revert: {chainId}, {cacheCount}, {graphQLCount}, {esCount}", chainId, txnHashList.Count, txnHashs.Count, tradeRecordList.Count);
            revertTxnHashList.AddRange(txnHashList.FindAll(t => !txnHashs.Contains(t.TransactionHash)));
            var revertTradeRecordList = tradeRecordList.FindAll(t => !txnHashs.Contains(t.TransactionHash));
            await RevertActionAsync(chainId, revertTxnHashList, revertTradeRecordList);
            
            var txns = txnHashList.FindAll(t => t.Retry == _tradeRecordOptions.RetryLimit).Select(t => t.TransactionHash).ToList();
            var intersectTxnHashList = txnHashs.Intersect(txns).ToList();
            var intersectTradeRecordList = tradeRecordList.FindAll(t => intersectTxnHashList.Contains(t.TransactionHash));
            intersectTradeRecordList.ForEach(t => t.IsConfirmed = true);
            await ConfirmActionAsync(chainId, intersectTxnHashList, intersectTradeRecordList);
        }
        
        private async Task RevertActionAsync(string chainId, List<TransactionHashDto> revertTxnHashList, List<Index.TradeRecord> revertTradeRecordList)
        {
            _logger.LogInformation("revert txnHash when revert: {chainId}, {revertTradeRecordListCount}, {revertTxnHashListCount}", chainId, revertTradeRecordList.Count, revertTxnHashList.Count);
            if(revertTradeRecordList.Count > 0) {
                await _tradeRecordIndexRepository.BulkDeleteAsync(revertTradeRecordList);
            }
            
            if (revertTxnHashList.Count == 0) return;
            
            try
            {
                await _transactionHashCache.RemoveManyAsync(revertTxnHashList.ConvertAll(t =>
                    $"{chainId}:{TradeRecordOptions.TransactionHashPrefix}:{t.TransactionHash}"));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "remove revert cache fail when revert.");
            }
            
            var listDto = new List<TradeRecordRemovedDto>();
            foreach (var revertTxnHash in revertTxnHashList)
            {
                listDto.Add(new TradeRecordRemovedDto()
                {
                    ChainId = chainId,
                    TradePairId = revertTxnHash.TradePairId,
                    Address = revertTxnHash.Address,
                    TransactionHash = revertTxnHash.TransactionHash
                });
            }

            await _bus.Publish<RemovedIndexEvent<TradeRecordRemovedListResultDto>>(new RemovedIndexEvent<TradeRecordRemovedListResultDto>
            {
                Data = new TradeRecordRemovedListResultDto()
                {
                    Items = listDto
                }
            });
            
            /*await _distributedEventBus.PublishAsync(new RemovedIndexEvent<TradeRecordRemovedListResultDto>
            {
                Data = new TradeRecordRemovedListResultDto()
                {
                    Items = listDto
                }
            });*/
        }

        private async Task ConfirmActionAsync(string chainId, List<string> intersectTxnHashList, List<Index.TradeRecord> intersectTradeRecordList)
        {
            _logger.LogInformation("confirm txnHash when revert: {chainId}, {intersectTradeRecordListCount}, {intersectTxnHashListCount}", chainId, intersectTradeRecordList.Count, intersectTxnHashList.Count);
            if(intersectTradeRecordList.Count > 0) {
                await _tradeRecordIndexRepository.BulkAddOrUpdateAsync(intersectTradeRecordList);
            }
            
            if (intersectTxnHashList.Count == 0) return;
            
            try
            {
                await _transactionHashCache.RemoveManyAsync(intersectTxnHashList
                    .ConvertAll(t => $"{chainId}:{TradeRecordOptions.TransactionHashPrefix}:{t}"));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "remove confirmed cache fail when revert.");
            }
        }
    }
}