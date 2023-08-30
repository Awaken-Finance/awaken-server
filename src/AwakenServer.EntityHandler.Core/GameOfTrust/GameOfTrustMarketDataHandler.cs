using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities.GameOfTrust.Es;
using AwakenServer.ETOs.GameOfTrust;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.GameOfTrust
{
    public class GameOfTrustMarketDataHandler :
        IDistributedEventHandler<EntityUpdatedEto<GameOfTrustMarketDataSnapshotEto>>,
        IDistributedEventHandler<EntityCreatedEto<GameOfTrustMarketDataSnapshotEto>>, ITransientDependency
    {
        private readonly INESTRepository<GameOfTrustMarketData, Guid> _marketRepository;
        private readonly IAutoObjectMappingProvider _mapper;

        public GameOfTrustMarketDataHandler(INESTRepository<GameOfTrustMarketData, Guid> marketRepository,
            IAutoObjectMappingProvider mapper)
        {
            _marketRepository = marketRepository;
            _mapper = mapper;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<GameOfTrustMarketDataSnapshotEto> eventData)
        {
            var marketDataSnapshot = eventData.Entity;
            var gameOfTrustMarketData =
                _mapper.Map<GameOfTrustMarketData, GameOfTrustMarketDataSnapshotEto>(marketDataSnapshot);
            await _marketRepository.UpdateAsync(gameOfTrustMarketData);
        }

        public async Task HandleEventAsync(EntityCreatedEto<GameOfTrustMarketDataSnapshotEto> eventData)
        {
            var marketDataEs = eventData.Entity;
            var gameOfTrustMarketData =
                _mapper.Map<GameOfTrustMarketData, GameOfTrustMarketDataSnapshotEto>(marketDataEs);
            await _marketRepository.AddAsync(gameOfTrustMarketData);
        }
    }
}