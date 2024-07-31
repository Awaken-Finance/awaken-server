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
using MongoDB.Bson.IO;
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
using JsonConvert = Newtonsoft.Json.JsonConvert;

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
        private readonly IBus _bus;
        private readonly IRevertProvider _revertProvider;
        private readonly KLinePeriodOptions _kLinePeriodOptions;
        private readonly INESTRepository<Index.KLine, Guid> _kLineIndexRepository;


        private const string ASC = "asc";
        private const string ASCEND = "ascend";
        private const string TIMESTAMP = "timestamp";
        private const string TRADEPAIR = "tradepair";
        private const string SIDE = "side";
        private const string TOTALPRICEINUSD = "totalpriceinusd";
        
        private const string ExactInMethodName= "SwapExactTokensForTokens";
        private const string ExactOutMethodName= "SwapTokensForExactTokens";
        
        public TradeRecordAppService(INESTRepository<Index.TradeRecord, Guid> tradeRecordIndexRepository,
            INESTRepository<Index.UserTradeSummary, Guid> userTradeSummaryIndexRepository,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository,
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            ILocalEventBus localEventBus,
            ILogger<TradeRecordAppService> logger,
            IOptionsSnapshot<TradeRecordRevertWorkerSettings> tradeRecordOptions,
            IDistributedEventBus distributedEventBus,
            IBus bus,
            IRevertProvider revertProvider,
            IOptionsSnapshot<KLinePeriodOptions> kLinePeriodOptions,
            INESTRepository<Index.KLine, Guid> kLineIndexRepository)
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
            _bus = bus;
            _revertProvider = revertProvider;
            _kLinePeriodOptions = kLinePeriodOptions.Value;
            _kLineIndexRepository = kLineIndexRepository;

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
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradeRecord> f)
            {
                var shouldQuery = new List<Func<QueryContainerDescriptor<Index.TradeRecord>, QueryContainer>>
                {
                    q => q.Term(t => t.Field(f => f.IsSubRecord).Value(false)),
                    q => q.Bool(b => b.MustNot(mn => mn.Exists(e => e.Field(f => f.IsSubRecord))))
                };

                return f.Bool(b => b
                    .Must(mustQuery)
                    .Filter(ff => ff.Bool(bf => bf
                        .Should(shouldQuery)
                        .MinimumShouldMatch(1)
                    ))
                );
            }
            
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
            foreach (var tradeRecord in item2.Where(t => t.Side == TradeSide.Swap))
            {
                tradeRecord.Price = 1 / tradeRecord.Price;
            }

            var totalCount = await _tradeRecordIndexRepository.CountAsync(Filter);

            return new PagedResultDto<TradeRecordIndexDto>
            {
                Items = ObjectMapper.Map<List<Index.TradeRecord>, List<TradeRecordIndexDto>>(item2),
                TotalCount = totalCount.Count
            };
        }
        
        public async Task<PagedResultDto<TradeRecordIndexDto>> GetListWithSubRecordsAsync(GetTradeRecordsInput input)
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

        public async Task<bool> WriteKLineIndexAsync(TradeRecord dto)
        {
            var timeStamp = DateTimeHelper.ToUnixTimeMilliseconds(dto.Timestamp);
            foreach (var period in _kLinePeriodOptions.Periods)
            {
                var periodTimestamp = KLineHelper.GetKLineTimestamp(period, timeStamp);
                var token0Amount = double.Parse(dto.Token0Amount);

                var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(dto.TradePairId));
                var tradePairResult = await tradePairGrain.GetAsync();
                if (!tradePairResult.Success)
                {
                    _logger.LogError($"fill kline, can't find trade pair: {dto.TradePairId}");
                    continue;
                }
                
                var priceWithoutFee = dto.Side == TradeSide.Buy
                    ? dto.Price * (1-tradePairResult.Data.FeeRate)
                    : dto.Price / (1-tradePairResult.Data.FeeRate);
                
                var kLine = new KLineGrainDto
                {
                    ChainId = dto.ChainId,
                    TradePairId = dto.TradePairId,
                    Open = dto.Price,
                    Close = dto.Price,
                    High = dto.Price,
                    Low = dto.Price,
                    OpenWithoutFee = priceWithoutFee,
                    CloseWithoutFee = priceWithoutFee,
                    HighWithoutFee = priceWithoutFee,
                    LowWithoutFee = priceWithoutFee,
                    Volume = token0Amount,
                    Period = period,
                    Timestamp = periodTimestamp
                };
              
                var id = GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TradePairId, period, "resync");
                var grain = _clusterClient.GetGrain<IKLineGrain>(id);
                var result = await grain.AddOrUpdateAsync(kLine);
                if (result.Success)
                {
                    var existIndex = await _kLineIndexRepository.GetAsync(q =>
                        q.Term(i => i.Field(f => f.ChainId).Value(dto.ChainId)) &&
                        q.Term(i => i.Field(f => f.TradePairId).Value(dto.TradePairId)) &&
                        q.Term(i => i.Field(f => f.Period).Value(kLine.Period)) &&
                        q.Term(i => i.Field(f => f.Timestamp).Value(kLine.Timestamp)));

                    if (existIndex != null)
                    {
                        _logger.LogInformation($"fill kline, existIndex: {JsonConvert.SerializeObject(existIndex)}, " +
                                               $"OpenWithoutFee: {result.Data.OpenWithoutFee}, " +
                                               $"CloseWithoutFee: {result.Data.CloseWithoutFee}, " +
                                               $"HighWithoutFee: {result.Data.HighWithoutFee}, " +
                                               $"LowWithoutFee: {result.Data.LowWithoutFee}");
                        existIndex.OpenWithoutFee = result.Data.OpenWithoutFee;
                        existIndex.CloseWithoutFee = result.Data.CloseWithoutFee;
                        existIndex.HighWithoutFee = result.Data.HighWithoutFee;
                        existIndex.LowWithoutFee = result.Data.LowWithoutFee;
                        await _kLineIndexRepository.AddOrUpdateAsync(existIndex); 
                    }
                }
            }

            return true;
        }
        
        public async Task<bool> FillKLineIndexAsync(SwapRecordDto dto)
        {
            dto.SwapRecords.AddFirst(new Dtos.SwapRecord()
            {
                PairAddress = dto.PairAddress,
                AmountIn = dto.AmountIn,
                AmountOut = dto.AmountOut,
                SymbolIn = dto.SymbolIn,
                SymbolOut = dto.SymbolOut,
                TotalFee = dto.TotalFee,
                Channel = dto.Channel
            });
            
            var pairList = new List<Index.TradePair>();
            var indexSwapRecords = new List<SwapRecord>();
            for (var i = 0; i < dto.SwapRecords.Count; i++)
            {
                var swapRecord = dto.SwapRecords[i];
                var tradePair = await GetAsync(dto.ChainId, swapRecord.PairAddress);
                if (tradePair == null)
                {
                    _logger.LogInformation("fill kline index can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                        swapRecord.PairAddress);
                    return false;
                } 
                pairList.Add(tradePair);
                var indexSwapRecord = new SwapRecord();
                ObjectMapper.Map(swapRecord, indexSwapRecord);
                indexSwapRecord.TradePairId = tradePair.Id;
                indexSwapRecord.TradePair = tradePair;
                indexSwapRecords.Add(indexSwapRecord);
            }
            
            var record = new TradeRecordCreateDto
            {
                ChainId = dto.ChainId,
                TradePairId = Guid.Empty,
                Address = dto.Sender,
                TransactionHash = dto.TransactionHash,
                Timestamp = dto.Timestamp,
                Channel = dto.Channel,
                Sender = dto.Sender,
                BlockHeight = dto.BlockHeight,
                MethodName = dto.MethodName
            };
            
            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(record);
            tradeRecord.Id = Guid.NewGuid();
            
            foreach (var indexSwapRecord in indexSwapRecords)
            {
                var pair = pairList.First(t => t.Id == indexSwapRecord.TradePairId);
                var isSell = pair.Token0.Symbol == indexSwapRecord.SymbolIn;
                tradeRecord.TradePairId = indexSwapRecord.TradePairId;
                tradeRecord.Side = isSell ? TradeSide.Sell : TradeSide.Buy;
                tradeRecord.Token0Amount = isSell
                    ? indexSwapRecord.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : indexSwapRecord.AmountOut.ToDecimalsString(pair.Token0.Decimals);
                tradeRecord.Token1Amount = isSell 
                    ? indexSwapRecord.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : indexSwapRecord.AmountIn.ToDecimalsString(pair.Token1.Decimals);
                tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
                tradeRecord.TotalFee = indexSwapRecord.TotalFee;
                await WriteKLineIndexAsync(tradeRecord);
            }

            return true;
        }
        
        public async Task<bool> CreateAsync(long currentConfirmedHeight, SwapRecordDto dto)
        {
            if (!dto.SwapRecords.IsNullOrEmpty())
            {
                return await CreateMultiSwapAsync(currentConfirmedHeight, dto);
            }
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(
                    GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (await tradeRecordGrain.Exist())
            {
                return true;
            }
            
            await _revertProvider.CheckOrAddUnconfirmedTransaction(currentConfirmedHeight, EventType.SwapEvent, dto.ChainId, dto.BlockHeight, dto.TransactionHash);

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

        public Tuple<double, double, double, long, long> GetDistributionsSum(List<List<SwapRecord>> indexSwapRecordDistributions, List<Index.TradePair> pairList)
        {
            double amount0 = 0;
            double amount1 = 0;
            double totalFee = 0;
            long amountInSum = 0;
            long amountOutSum = 0;
            foreach (var swapRecords in indexSwapRecordDistributions)
            {
                if (swapRecords.Count < 1)
                {
                    continue;
                }

                var firstSwapRecord = swapRecords.First();
                var lastSwapRecord = swapRecords.Last();
                var firstTradePair = pairList.First(t => t.Id == firstSwapRecord.TradePairId);
                var lastTradePair = pairList.First(t => t.Id == lastSwapRecord.TradePairId);
                var firstIsSell = firstTradePair.Token0.Symbol == firstSwapRecord.SymbolIn;
                var lastIsSell = lastTradePair.Token0.Symbol == lastSwapRecord.SymbolIn;
                var currentAmount0 = firstIsSell
                    ? firstSwapRecord.AmountIn.ToDecimalsString(firstTradePair.Token0.Decimals)
                    : firstSwapRecord.AmountIn.ToDecimalsString(firstTradePair.Token1.Decimals);
                var currentAmount1 = lastIsSell
                    ? lastSwapRecord.AmountOut.ToDecimalsString(lastTradePair.Token1.Decimals)
                    : lastSwapRecord.AmountOut.ToDecimalsString(lastTradePair.Token0.Decimals);
                
                var feeRateRest = 1d;
                foreach (var swapRecord in swapRecords)
                {
                    var tradePair = pairList.First(t => t.Id == swapRecord.TradePairId);
                    feeRateRest *= 1 - tradePair.FeeRate;
                }
                
                var currentTotalFee = firstSwapRecord.AmountIn / Math.Pow(10,
                                          firstIsSell ? firstTradePair.Token0.Decimals : firstTradePair.Token1.Decimals)
                                      * (1 - feeRateRest);
                
                amount0 += double.Parse(currentAmount0);
                amount1 += double.Parse(currentAmount1);
                totalFee += currentTotalFee;
                amountInSum += firstSwapRecord.AmountIn;
                amountOutSum += lastSwapRecord.AmountOut;
            }

            return new Tuple<double, double, double, long, long>(amount0, amount1, totalFee, amountInSum, amountOutSum);
        }

        public List<PercentRoute> GetPercentRoutes(string methodName, List<List<SwapRecord>> indexSwapRecordDistributions, long amountInSum, long amountOutSum)
        {
            var result = new List<PercentRoute>();
            bool percentDependsOnIn = methodName == ExactInMethodName;
            foreach (var swapRecords in indexSwapRecordDistributions)
            {
                if (swapRecords.Count < 1)
                {
                    continue;
                }
                var firstSwapRecord = swapRecords.First();
                var lastSwapRecord = swapRecords.Last();
                var percent = percentDependsOnIn
                    ? amountInSum == 0 ? 0 : firstSwapRecord.AmountIn / (double)amountInSum
                    : amountOutSum == 0 ? 0 : lastSwapRecord.AmountOut / (double)amountOutSum;
                result.Add(new PercentRoute()
                {
                    Percent = (percent * 100).ToString("F0"),
                    Route = swapRecords
                });
            }

            return result;
        }
        
        public async Task<bool> CreateMultiSwapAsync(long currentConfirmedHeight, SwapRecordDto dto)
        {
            _logger.LogInformation($"creare multi swap records begin, chain: {dto.ChainId}, txn: {dto.TransactionHash}, swap count: {dto.SwapRecords.Count+1}, method name: {dto.MethodName}");
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(
                    GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (await tradeRecordGrain.Exist())
            {
                return true;
            }
            
            await _revertProvider.CheckOrAddUnconfirmedTransaction(currentConfirmedHeight, EventType.SwapEvent, dto.ChainId, dto.BlockHeight, dto.TransactionHash);
            
            dto.SwapRecords.AddFirst(new Dtos.SwapRecord()
            {
                PairAddress = dto.PairAddress,
                AmountIn = dto.AmountIn,
                AmountOut = dto.AmountOut,
                SymbolIn = dto.SymbolIn,
                SymbolOut = dto.SymbolOut,
                TotalFee = dto.TotalFee,
                Channel = dto.Channel
            });
            var pairList = new List<Index.TradePair>();
            var indexSwapRecords = new List<SwapRecord>();
            var currentIndexSwapRecords = new List<SwapRecord>();
            var indexSwapRecordDistributions = new List<List<SwapRecord>>();
            var symbolInSet = new HashSet<string>() {dto.SwapRecords[0].SymbolIn};
            for (var i = 0; i < dto.SwapRecords.Count; i++)
            {
                var swapRecord = dto.SwapRecords[i];
                var tradePair = await GetAsync(dto.ChainId, swapRecord.PairAddress);
                if (tradePair == null)
                {
                    _logger.LogInformation("creare multi swap records can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                        swapRecord.PairAddress);
                    return false;
                } 
                pairList.Add(tradePair);
                var indexSwapRecord = new SwapRecord();
                ObjectMapper.Map(swapRecord, indexSwapRecord);
                indexSwapRecord.TradePairId = tradePair.Id;
                indexSwapRecord.TradePair = tradePair;
                indexSwapRecords.Add(indexSwapRecord);
                if (symbolInSet.Contains(swapRecord.SymbolIn) && i > 0)
                {
                    indexSwapRecordDistributions.Add(currentIndexSwapRecords);
                    currentIndexSwapRecords = new List<SwapRecord>() { indexSwapRecord };
                }
                else
                {
                    currentIndexSwapRecords.Add(indexSwapRecord);
                }
            }
            indexSwapRecordDistributions.Add(currentIndexSwapRecords);
            
            var (amount0, amount1, totalFee, amountInSum, amountOutSum) = GetDistributionsSum(indexSwapRecordDistributions, pairList);
            
            var record = new TradeRecordCreateDto
            {
                ChainId = dto.ChainId,
                TradePairId = Guid.Empty,
                Address = dto.Sender,
                TransactionHash = dto.TransactionHash,
                Timestamp = dto.Timestamp,
                Side = TradeSide.Swap,
                Token0Amount = amount0.ToString(),
                Token1Amount = amount1.ToString(),
                TotalFee = totalFee,
                Channel = dto.Channel,
                Sender = dto.Sender,
                BlockHeight = dto.BlockHeight,
                MethodName = dto.MethodName
            };

            _logger.LogInformation(
                "creare multi swap records, input chainId: {chainId}, tradePairId: {tradePairId}, address: {address}, " +
                "transactionHash: {transactionHash}, timestamp: {timestamp}, side: {side}, channel: {channel}, token0Amount: {token0Amount}, token1Amount: {token1Amount}, " +
                "blockHeight: {blockHeight}, totalFee: {totalFee}, MethodName: {MethodName}", dto.ChainId, "multiSwap no tradePairId", dto.Sender,
                dto.TransactionHash, dto.Timestamp,
                record.Side, dto.Channel, record.Token0Amount, record.Token1Amount, dto.BlockHeight, dto.TotalFee, record.MethodName);
            
            var tradeRecord = ObjectMapper.Map<TradeRecordCreateDto, TradeRecord>(record);
            tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
            tradeRecord.Id = Guid.NewGuid();
            tradeRecord.SwapRecords = indexSwapRecords;
            tradeRecord.PercentRoutes = GetPercentRoutes(record.MethodName, indexSwapRecordDistributions, amountInSum, amountOutSum);
            
            _logger.LogInformation($"creare multi swap records, transactionHash: {dto.TransactionHash}, " +
                                   $"MethodName: {record.MethodName}, " +
                                   $"PercentRoutes: {JsonConvert.SerializeObject(tradeRecord.PercentRoutes)}");
            
            await tradeRecordGrain.InsertAsync(ObjectMapper.Map<TradeRecord, TradeRecordGrainDto>(tradeRecord));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<MultiTradeRecordEto>(
                ObjectMapper.Map<TradeRecord, MultiTradeRecordEto>(tradeRecord)
            ));

            foreach (var indexSwapRecord in indexSwapRecords)
            {
                record.TradePairId = indexSwapRecord.TradePairId;
                await CreateUserTradeSummary(record);

                var pair = pairList.First(t => t.Id == indexSwapRecord.TradePairId);
                var isSell = pair.Token0.Symbol == indexSwapRecord.SymbolIn;
                tradeRecord.TradePairId = indexSwapRecord.TradePairId;
                tradeRecord.Side = isSell ? TradeSide.Sell : TradeSide.Buy;
                tradeRecord.Token0Amount = isSell
                    ? indexSwapRecord.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : indexSwapRecord.AmountOut.ToDecimalsString(pair.Token0.Decimals);
                tradeRecord.Token1Amount = isSell 
                    ? indexSwapRecord.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : indexSwapRecord.AmountIn.ToDecimalsString(pair.Token1.Decimals);
                tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
                tradeRecord.TotalFee = indexSwapRecord.TotalFee;
                await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));
            }
            
            _logger.LogInformation($"creare multi swap records done, chain: {dto.ChainId}, txn: {dto.TransactionHash}, swap count: {dto.SwapRecords.Count+1}");
            return true;
        }

        public async Task<bool> RevertFieldMultiAsync(Index.TradeRecord dto)
        {
            var tradeRecordGrain =
                _clusterClient.GetGrain<ITradeRecordGrain>(
                    GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash));
            if (!tradeRecordGrain.Exist().Result)
            {
                _logger.LogInformation("revert transactionHash not existed: {transactionHash}", dto.TransactionHash);
                return false;
            }
            
            var pairList = new List<Index.TradePair>();
            foreach (var swapRecord in dto.SwapRecords)
            {
                var tradePair = await GetAsync(dto.ChainId, swapRecord.PairAddress);
                if (tradePair == null)
                {
                    _logger.LogInformation("swap can not find trade pair: {chainId}, {pairAddress}", dto.ChainId,
                        swapRecord.PairAddress);
                    return false;
                }

                pairList.Add(tradePair);
            }
            
            var tradeRecord = ObjectMapper.Map<Index.TradeRecord, TradeRecord>(dto);
            tradeRecord.IsRevert = true;
            
            foreach (var swapRecord in dto.SwapRecords)
            {
                var pair = pairList.First(t => t.Id == swapRecord.TradePairId);
                var isSell = pair.Token0.Symbol == swapRecord.SymbolIn;
                tradeRecord.TradePairId = swapRecord.TradePairId;
                tradeRecord.Side = isSell ? TradeSide.Sell : TradeSide.Buy;
                tradeRecord.Token0Amount = isSell
                    ? swapRecord.AmountIn.ToDecimalsString(pair.Token0.Decimals)
                    : swapRecord.AmountOut.ToDecimalsString(pair.Token0.Decimals);
                tradeRecord.Token1Amount = isSell 
                    ? swapRecord.AmountOut.ToDecimalsString(pair.Token1.Decimals)
                    : swapRecord.AmountIn.ToDecimalsString(pair.Token1.Decimals);
                tradeRecord.Price = double.Parse(tradeRecord.Token1Amount) / double.Parse(tradeRecord.Token0Amount);
                
                // update kLine and trade pair by publish event : NewTradeRecordEvent, Handler: KLineHandler and kNewTradeRecordHandler
                await _localEventBus.PublishAsync(ObjectMapper.Map<TradeRecord, NewTradeRecordEvent>(tradeRecord));
            
                // update trade pair token0reserved, token1reserved, price ... from chain
            }

            return true;
        }
        

        public async Task<bool> RevertFieldAsync(Index.TradeRecord dto)
        {
            if (!dto.SwapRecords.IsNullOrEmpty())
            {
                return await RevertFieldMultiAsync(dto);
            }
            
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
            
            // update trade pair token0reserved, token1reserved, price ... from chain
            
            return true;
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
        
        public async Task<List<Index.TradePair>> GetTradePairListFromEsAsync(string chainId, IEnumerable<string> addresses)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                q.Terms(i => i.Field(f => f.Address).Terms(addresses));
            
            var list = await _tradePairIndexRepository.GetListAsync(Filter,
                limit: addresses.Count(), skip: 0);
            
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