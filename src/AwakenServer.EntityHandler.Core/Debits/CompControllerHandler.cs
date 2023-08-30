using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Debits.Entities.Es;
using AwakenServer.ETOs.Debits;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Debits
{
    public class CompControllerHandler: IDistributedEventHandler<EntityUpdatedEto<CompControllerChangedEto>>, ITransientDependency
    {
        private readonly INESTRepository<CompController, Guid> _compControllerRepository;

        public CompControllerHandler(INESTRepository<CompController, Guid> compControllerRepository)
        {
            _compControllerRepository = compControllerRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CompControllerChangedEto> eventData)
        {
            CompController compController = eventData.Entity;
            await _compControllerRepository.AddOrUpdateAsync(compController);
        }
    }
}