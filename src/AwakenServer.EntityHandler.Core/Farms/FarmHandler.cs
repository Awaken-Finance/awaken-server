using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Es;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Microsoft.Extensions.Logging;

namespace AwakenServer.EntityHandler.Farms
{
    public class FarmHandler : IDistributedEventHandler<EntityUpdatedEto<FarmChangedEto>>, ITransientDependency
    {
        private readonly INESTRepository<Farm, Guid> _farmRepository;
        private readonly ILogger<FarmHandler> _logger;

        public FarmHandler(INESTRepository<Farm, Guid> farmRepository, ILogger<FarmHandler> logger)
        {
            _farmRepository = farmRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<FarmChangedEto> eventData)
        {
            var esFarm = eventData.Entity;
            await _farmRepository.AddOrUpdateAsync(esFarm);
        }
    }
}