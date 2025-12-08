using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using Nethereum.Util;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

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
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger _logger;
        private readonly IClusterClient _clusterClient;

        private static DateTime lastWriteTime;

        private static BigDecimal lastTotal;

        public TradePairMarketDataProvider(
            INESTRepository<Index.TradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper,
            IClusterClient clusterClient)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _logger = Log.ForContext<TradePairMarketDataProvider>();
            _clusterClient = clusterClient;
        }
        
        public async Task AddOrUpdateSnapshotAsync(Guid tradePairId, TradePairMethodDelegate methodDelegate)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairId));
            if (!(await grain.GetAsync()).Success)
            {
                _logger.Information("trade pair: {tradePairId} not exist", tradePairId);
                return;
            }
            
            var result = await methodDelegate(grain);
            
            _logger.Debug("from {name} publishAsync TradePairEto: {tradePairEto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.TradePairDto));
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairEto>(
                _objectMapper.Map<TradePairGrainDto, TradePairEto>(
                    result.Data.TradePairDto)
            ));
            
            _logger.Debug("from {name} publishAsync TradePairMarketDataSnapshotEto: {tradePairMarketDataSnapshotEto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.SnapshotDto));

            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairMarketDataSnapshotEto>(
                _objectMapper.Map<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotEto>(
                    result.Data.SnapshotDto)
            ));

            if (result.Data.LatestSnapshotDto != null)
            {
                _logger.Debug("update latest snapshot from {name} publishAsync TradePairMarketDataSnapshotEto: {latestSnapshotDto}", methodDelegate.Method.Name, JsonConvert.SerializeObject(result.Data.LatestSnapshotDto));
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