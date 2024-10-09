using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Proto;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Comparers;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Nethereum.Util;
using Orleans;
using Serilog;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Trade
{
    public delegate Task<GrainResultDto<TradePairMarketDataSnapshotUpdateResult>> TradePairMethodDelegate(ITradePairGrain grain);
    
    public interface ITradePairMarketDataProvider
    {
        Task AddOrUpdateSnapshotAsync(Guid tradePairId, 
            TradePairMethodDelegate methodDelegate);
        
        Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId, Guid tradePairId,
            DateTime snapshotTime);

        DateTime GetSnapshotTime(DateTime time);
        
        Task<TradePairMarketDataSnapshotGrainDto> GetLatestTradePairMarketDataFromGrainAsync(string chainId,
            Guid tradePairId);
        
    }

    public class TradePairMarketDataProvider : ITransientDependency, ITradePairMarketDataProvider
    {
        private readonly INESTRepository<Index.TradePairMarketDataSnapshot, Guid> _snapshotIndexRepository;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IObjectMapper _objectMapper;
        private readonly IBus _bus;
        private readonly ILogger<TradePairMarketDataProvider> _logger;
        private readonly IAbpDistributedLock _distributedLock;
        private readonly IClusterClient _clusterClient;
        private readonly IAElfClientProvider _blockchainClientProvider;
        private readonly ContractsTokenOptions _contractsTokenOptions;

        private static DateTime lastWriteTime;

        private static BigDecimal lastTotal;

        public TradePairMarketDataProvider(
            INESTRepository<Index.TradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository,
            ITradeRecordAppService tradeRecordAppService,
            IDistributedEventBus distributedEventBus,
            IBus bus,
            IObjectMapper objectMapper,
            IAbpDistributedLock distributedLock,
            ILogger<TradePairMarketDataProvider> logger,
            IClusterClient clusterClient,
            IAElfClientProvider blockchainClientProvider, IOptions<ContractsTokenOptions> contractsTokenOptions)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradeRecordAppService = tradeRecordAppService;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _bus = bus;
            _distributedLock = distributedLock;
            _logger = logger;
            _clusterClient = clusterClient;
            _blockchainClientProvider = blockchainClientProvider;
            _contractsTokenOptions = contractsTokenOptions.Value;
        }
        
        public async Task AddOrUpdateSnapshotAsync(Guid tradePairId, TradePairMethodDelegate methodDelegate)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairId));
            if (!(await grain.GetAsync()).Success)
            {
                Log.Information("trade pair: {tradePairId} not exist", tradePairId);
                return;
            }
            
            var result = await methodDelegate(grain);
            
            Log.Debug("from {name} publishAsync TradePairEto: {tradePairEto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.TradePairDto));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairEto>(
                _objectMapper.Map<TradePairGrainDto, TradePairEto>(
                    result.Data.TradePairDto)
            ));
            
            Log.Debug("from {name} publishAsync TradePairMarketDataSnapshotEto: {tradePairMarketDataSnapshotEto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.SnapshotDto));

            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairMarketDataSnapshotEto>(
                _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotEto>(
                    result.Data.SnapshotDto)
            ));

            if (result.Data.LatestSnapshotDto != null)
            {
                Log.Debug("update latest snapshot from {name} publishAsync TradePairMarketDataSnapshotEto: {latestSnapshotDto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.LatestSnapshotDto));
                await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairMarketDataSnapshotEto>(
                    _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotEto>(
                        result.Data.LatestSnapshotDto)
                ));
            }
        }
        

        public DateTime GetSnapshotTime(DateTime time)
        {
            return time.Date.AddHours(time.Hour);
        }

        public async Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId,
            Guid tradePairId, DateTime snapshotTime)
        {
            return await _snapshotIndexRepository.GetAsync(
                q => q.Term(i => i.Field(f => f.ChainId).Value(chainId))
                     && q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId))
                     && q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
        }
        
        
        public async Task<TradePairMarketDataSnapshotGrainDto> GetLatestTradePairMarketDataFromGrainAsync(
            string chainId,
            Guid tradePairId)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairId));
            return await grain.GetLatestSnapshotAsync();
        }
    }
}