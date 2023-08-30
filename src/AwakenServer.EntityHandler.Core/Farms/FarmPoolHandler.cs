using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Es;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Farms
{
    public class FarmPoolHandler : IDistributedEventHandler<EntityUpdatedEto<FarmPoolChangedEto>>,
        IDistributedEventHandler<EntityCreatedEto<FarmPoolChangedEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<FarmPool, Guid> _poolRepository;
        private readonly ILogger<FarmPoolHandler> _logger;

        public FarmPoolHandler(INESTRepository<FarmPool, Guid> poolRepository, ILogger<FarmPoolHandler> logger)
        {
            _poolRepository = poolRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<FarmPoolChangedEto> eventData)
        {
            var pool = eventData.Entity;
            _logger.LogInformation($"pool updated, id: {pool.Id}  , farmId: {pool.FarmId} , chainId: {pool.ChainId}");
            await _poolRepository.UpdateAsync(pool);
        }

        public async Task HandleEventAsync(EntityCreatedEto<FarmPoolChangedEto> eventData)
        {
            var pool = eventData.Entity;
            _logger.LogInformation($"pool created, id: {pool.Id}  , farmId: {pool.FarmId} , chainId: {pool.ChainId}");
            await _poolRepository.AddAsync(pool);
        }
    }
}