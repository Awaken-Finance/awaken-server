using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Common;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Provider;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Worker;
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
        private readonly TradeRecordRevertWorkerSettings _tradeRecordRevertWorkerOptions;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly IBus _bus;
        private readonly IRevertProvider _revertProvider;
        private readonly IAElfClientProvider _aelfClientProvider;


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
            IOptionsSnapshot<TradeRecordRevertWorkerSettings> tradeRecordOptions,
            IDistributedEventBus distributedEventBus,
            IGraphQLProvider graphQlProvider,
            IBus bus,
            IAElfClientProvider aefClientProvider,
            IRevertProvider revertProvider)
        {
            _tradeRecordIndexRepository = tradeRecordIndexRepository;
            _userTradeSummaryIndexRepository = userTradeSummaryIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _localEventBus = localEventBus;
            _logger = logger;
            _tradeRecordRevertWorkerOptions = tradeRecordOptions.Value;
            _distributedEventBus = distributedEventBus;
            _graphQlProvider = graphQlProvider;
            _bus = bus;
            _revertProvider = revertProvider;
            _aelfClientProvider = aefClientProvider;
        }

        public async Task<TradeRecordIndexDto> GetRecordAsync(string transactionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionHash).Value(transactionId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));


            var record = await _tradeRecordIndexRepository.GetAsync(Filter);

            if (record == null)
            {
                return null;
            }

            return ObjectMapper.Map<Index.TradeRecord, TradeRecordIndexDto>(record);
        }

        public async Task<TradeRecordIndexDto> GetRecordFromGrainAsync(string chainId, string transactionId)
        {
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(GrainIdHelper.GenerateGrainId(chainId, transactionId));
            var result = await tradeRecordGrain.GetAsync();
            if (!result.Success)
            {
                return null;
            }

            return ObjectMapper.Map<TradeRecordGrainDto, TradeRecordIndexDto>(result.Data);
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
                    s => s.Wildcard(w =>
                        w.Field(f => f.TradePair.Token0.Symbol).Value($"*{input.TokenSymbol.ToUpper()}*")),
                    s => s.Wildcard(w =>
                        w.Field(f => f.TradePair.Token1.Symbol).Value($"*{input.TokenSymbol.ToUpper()}*")))));
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
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));

            List<Index.TradeRecord> item2;
            if (!string.IsNullOrEmpty(input.Sorting))
            {
                var sorting = GetSorting(input.Sorting);
                var list = await _tradeRecordIndexRepository.GetSortListAsync(Filter,
                    sortFunc: sorting,
                    limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
                    input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize :
                    input.MaxResultCount,
                    skip: input.SkipCount);
                item2 = list.Item2;
            }
            else
            {
                var list = await _tradeRecordIndexRepository.GetSortListAsync(Filter,
                    sortFunc: s => s.Descending(t => t.Timestamp),
                    limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
                    input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize :
                    input.MaxResultCount,
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
        
        public async Task CreateUserTradeSummary(TradeRecordCreateDto input)
        {
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
                    LatestTradeTime = DateTimeHelper.FromUnixTimeMilliseconds(input.Timestamp)
                };

                await userTradeSummaryGrain.AddOrUpdateAsync(userTradeSummary);
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<UserTradeSummaryGrainDto, UserTradeSummaryEto>(userTradeSummary)
                );
            }
            else
            {
                userTradeSummaryResult.Data.LatestTradeTime = DateTimeHelper.FromUnixTimeMilliseconds(input.Timestamp);
                await userTradeSummaryGrain.AddOrUpdateAsync(userTradeSummaryResult.Data);
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<UserTradeSummaryGrainDto, UserTradeSummaryEto>(userTradeSummaryResult.Data)
                );
            }
        }
        
        public async Task CreateAsync(TradeRecordCreateDto input)
        {
            var tradeRecordGrain = _clusterClient.GetGrain<ITradeRecordGrain>(GrainIdHelper.GenerateGrainId(input.ChainId, input.TransactionHash));
            if (await tradeRecordGrain.Exist())
            {
                return;
            }
            
            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(input);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();
            
            await tradeRecordGrain.InsertAsync(ObjectMapper.Map<TradeRecord, TradeRecordGrainDto>(tradeRecord));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradeRecordEto>(
                ObjectMapper.Map<TradeRecord, TradeRecordEto>(tradeRecord)
            ));

            await CreateUserTradeSummary(input);

            await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));
        }

        public async Task<bool> CreateAsync(SwapRecordDto dto)
        {
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(
                    GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (await tradeRecordGrain.Exist())
            {
                return true;
                // var recordResultDto = await tradeRecordGrain.GetAsync();
                // var tradeRecordIndex = await _tradeRecordIndexRepository.GetAsync(recordResultDto.Data.Id);
                // if (tradeRecordIndex != null)
                // {
                //     return true;
                // }
                // _logger.LogInformation($"swap record exist in grain does exist in es: {dto.Sender}, {dto.TransactionHash}");
            }
            
            await _revertProvider.CheckOrAddUnconfirmedTransaction(EventType.SwapEvent, dto.ChainId, dto.BlockHeight, dto.TransactionHash);

            var pair = await GetAsync(dto.ChainId, dto.PairAddress);
            if (pair == null)
            {
                _logger.LogInformation("swap can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                    dto.PairAddress);
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
                Token0Amount = isSell
                    ? dto.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : dto.AmountOut.ToDecimalsString(pair.Token0.Decimals),
                Token1Amount = isSell
                    ? dto.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : dto.AmountIn.ToDecimalsString(pair.Token1.Decimals),
                TotalFee = dto.TotalFee / Math.Pow(10, isSell ? pair.Token0.Decimals : pair.Token1.Decimals),
                Channel = dto.Channel,
                Sender = dto.Sender,
                BlockHeight = dto.BlockHeight
            };

            _logger.LogInformation(
                "SwapEvent, input chainId: {chainId}, tradePairId: {tradePairId}, address: {address}, " +
                "transactionHash: {transactionHash}, timestamp: {timestamp}, side: {side}, channel: {channel}, token0Amount: {token0Amount}, token1Amount: {token1Amount}, " +
                "blockHeight: {blockHeight}, totalFee: {totalFee}", dto.ChainId, pair.Id, dto.Sender,
                dto.TransactionHash, dto.Timestamp,
                record.Side, dto.Channel, record.Token0Amount, record.Token1Amount, dto.BlockHeight, dto.TotalFee);


            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(record);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();

            await tradeRecordGrain.InsertAsync(ObjectMapper.Map<TradeRecord, TradeRecordGrainDto>(tradeRecord));

            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradeRecordEto>(
                ObjectMapper.Map<TradeRecord, TradeRecordEto>(tradeRecord)
            ));

            await CreateUserTradeSummary(record);
            
            await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));

            return true;
        }


        public async Task<bool> RevertFieldAsync(Index.TradeRecord dto)
        {
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(
                    GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (!tradeRecordGrain.Exist().Result)
            {
                _logger.LogInformation("revert transactionHash not existed: {transactionHash}", dto.TransactionHash);
                return false;
            }

            var pair = await GetAsync(dto.ChainId, dto.TradePair.Address);
            if (pair == null)
            {
                _logger.LogInformation("revert can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                    dto.TradePair.Address);
                return false;
            }

            _logger.LogInformation(
                "Revert SwapEvent, input chainId: {chainId}, tradePairId: {tradePairId}, address: {address}, " +
                "transactionHash: {transactionHash}, timestamp: {timestamp}, side: {side}, channel: {channel}, token0Amount: {token0Amount}, token1Amount: {token1Amount}, " +
                "blockHeight: {blockHeight}, totalFee: {totalFee}", dto.ChainId, pair.Id, dto.Sender,
                dto.TransactionHash, dto.Timestamp,
                dto.Side, dto.Channel, dto.Token0Amount, dto.Token1Amount, dto.BlockHeight, dto.TotalFee);


            var tradeRecord = ObjectMapper.Map<Index.TradeRecord, TradeRecord>(dto);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();
            tradeRecord.IsRevert = true;

            // update kLine and trade pair by publish event : NewTradeRecordEvent, Handler: KLineHandler and kNewTradeRecordHandler
            await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));
            
            return true;
        }


        public async Task FillRecord(SwapRecordDto dto)
        {
            var pair = await GetAsync(dto.ChainId, dto.PairAddress);
            if (pair == null)
            {
                _logger.LogInformation("swap can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                    dto.PairAddress);
                return;
            }

            if (await GetRecordFromGrainAsync(dto.ChainId, dto.TransactionHash) != null)
            {
                _logger.LogInformation("FixTrade  record continue,blockHeight:{1}", dto.BlockHeight);
                return;
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
                Token0Amount = isSell
                    ? dto.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : dto.AmountOut.ToDecimalsString(pair.Token0.Decimals),
                Token1Amount = isSell
                    ? dto.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : dto.AmountIn.ToDecimalsString(pair.Token1.Decimals),
                TotalFee = dto.TotalFee / Math.Pow(10, isSell ? pair.Token0.Decimals : pair.Token1.Decimals),
                Channel = dto.Channel,
                Sender = dto.Sender,
                BlockHeight = dto.BlockHeight
            };


            _logger.LogInformation(
                "FixTrade SwapEvent, input chainId: {chainId}, tradePairId: {tradePairId}, address: {address}, " +
                "transactionHash: {transactionHash}, timestamp: {timestamp}, side: {side}, channel: {channel}, token0Amount: {token0Amount}, token1Amount: {token1Amount}, " +
                "blockHeight: {blockHeight}, totalFee: {totalFee}", dto.ChainId, pair.Id, dto.Sender,
                dto.TransactionHash, dto.Timestamp,
                record.Side, dto.Channel, record.Token0Amount, record.Token1Amount, dto.BlockHeight, dto.TotalFee);

            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(record);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();
            var tradeRecordGrain = _clusterClient.GetGrain<ITradeRecordGrain>(tradeRecord.TransactionHash);
            await tradeRecordGrain.InsertAsync(ObjectMapper.Map<TradeRecord, TradeRecordGrainDto>(tradeRecord));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradeRecordEto>(
                ObjectMapper.Map<TradeRecord, TradeRecordEto>(tradeRecord)
            ));
        }

        public async Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords)
        {
            if (needDeletedTradeRecords.IsNullOrEmpty())
            {
                return;
            }

            var needDeleteIndexes = await GetRecordAsync(chainId, needDeletedTradeRecords, _tradeRecordRevertWorkerOptions.QueryOnceLimit);
            foreach (var tradeRecord in needDeleteIndexes)
            {
                tradeRecord.IsDeleted = true;
            }
                
            await _tradeRecordIndexRepository.BulkAddOrUpdateAsync(needDeleteIndexes);

            var listDto = new List<TradeRecordRemovedDto>();
            foreach (var tradeRecord in needDeleteIndexes)
            {
                await RevertFieldAsync(tradeRecord);
                listDto.Add(new TradeRecordRemovedDto()
                {
                    ChainId = chainId,
                    TradePairId = tradeRecord.TradePair.Id,
                    Address = tradeRecord.Address,
                    TransactionHash = tradeRecord.TransactionHash
                });
            }

            await _bus.Publish(
                new RemovedIndexEvent<TradeRecordRemovedListResultDto>
                {
                    Data = new TradeRecordRemovedListResultDto()
                    {
                        Items = listDto
                    }
                });
        }

        public async Task BulkUpdateTxnFeeAsync(List<Index.TradeRecord> recordIndexes)
        {
            foreach (var tradeRecord in recordIndexes)
            {
                tradeRecord.TransactionFee = await _aelfClientProvider.GetTransactionFeeAsync(tradeRecord.ChainId, tradeRecord.TransactionHash) /
                                             Math.Pow(10, 8);
                _logger.LogInformation($"update trade record txn fee, {tradeRecord.TransactionHash}, {tradeRecord.TransactionFee}");
                
            }
                
            await _tradeRecordIndexRepository.BulkAddOrUpdateAsync(recordIndexes);
        }
        
        
        public async Task UpdateAllTxnFeeAsync(string chainId)
        {
            int pageSize = 1000; 
            int skipCount = 0;
            
            while (true)
            {
                List<Index.TradeRecord> pageData = await GetListAsync(chainId, skipCount, pageSize);

                if (pageData.Count == 0)
                {
                    break;
                }

                await BulkUpdateTxnFeeAsync(pageData);
                skipCount += pageData.Count;
            }
            
            _logger.LogInformation($"update all trade record txn fee end, affected records: {skipCount}");
        }
        
        
        public async Task RemoveDuplicatesAsync(string chainId)
        {
            int pageSize = 1000; 
            int skipCount = 0;
            DateTime timeOffset = DateTime.Parse("2024-05-12 18:00:00");
                
            Dictionary<string, List<Index.TradeRecord>> txnHashToList = new Dictionary<string, List<Index.TradeRecord>>();
            while (true)
            {
                List<Index.TradeRecord> pageData = await GetListAsync(chainId, skipCount, pageSize);

                if (pageData.Count == 0)
                {
                    break;
                }
                
                skipCount += pageData.Count;
                
                foreach (var record in pageData)
                {
                    if (record.Timestamp < timeOffset)
                    {
                        continue;
                    }
                    if (!txnHashToList.ContainsKey(record.TransactionHash))
                    {
                        txnHashToList[record.TransactionHash] = new List<Index.TradeRecord>();
                    }
                    txnHashToList[record.TransactionHash].Add(record);
                    _logger.LogInformation($"txn: {record.TransactionHash}, add: {record.Id}");
                }
            }

            List<Index.TradeRecord> duplicatesList = new List<Index.TradeRecord>();
            foreach (var record in txnHashToList)
            {
                if (record.Value.Count > 1)
                {
                    _logger.LogInformation($"txn :{record.Key}, count: {record.Value.Count}");
                    _logger.LogInformation($"txn :{record.Key}, keep: {record.Value[0].Id}");
                    for (int i = 1; i < record.Value.Count; i++)
                    {
                        var value = record.Value[i];
                        value.IsDeleted = true;
                        _logger.LogInformation($"txn :{record.Key}, remove: {value.Id}");
                        duplicatesList.Add(value);
                    }
                }
            }
            _logger.LogInformation($"RemoveDuplicatesAsync BulkAddOrUpdateAsync begin, affected records: {duplicatesList.Count}");
            if (duplicatesList.Count > 0)
            {
                await _tradeRecordIndexRepository.BulkAddOrUpdateAsync(duplicatesList);
            }
            _logger.LogInformation($"RemoveDuplicatesAsync BulkAddOrUpdateAsync end, affected records: {duplicatesList.Count}");
        }
        
        public async Task RevertTradeRecordAsync(string chainId)
        {
            try
            {
                var needDeletedTradeRecords =
                    await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.SwapEvent, chainId);

                await DoRevertAsync(chainId, needDeletedTradeRecords);
            }
            catch (Exception e)
            {
                _logger.LogError("Revert trade record err:{0}", e);
            }
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

        private async Task<List<Index.TradeRecord>> GetListAsync(string chainId, long blockHeight, int skipCount,
            int maxResultCount)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsConfirmed).Value(false)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(blockHeight)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradeRecordIndexRepository.GetListAsync(Filter, limit: maxResultCount, skip: skipCount,
                sortExp: m => m.BlockHeight);
            return list.Item2;
        }

        private async Task<List<Index.TradeRecord>> GetListAsync(string chainId, int skipCount,
            int maxResultCount)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradeRecordIndexRepository.GetListAsync(Filter, limit: maxResultCount, skip: skipCount,
                sortExp: m => m.BlockHeight);
            return list.Item2;
        }
        
        private async Task<List<Index.TradeRecord>> GetRecordAsync(string chainId, List<string> transactionHashs, int maxResultCount)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionHash).Terms(transactionHashs)));
            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f) => f.Bool(b => b.Must(mustQuery));

            var list = await _tradeRecordIndexRepository.GetListAsync(Filter, limit: maxResultCount,
                sortExp: m => m.BlockHeight);
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
    }
}