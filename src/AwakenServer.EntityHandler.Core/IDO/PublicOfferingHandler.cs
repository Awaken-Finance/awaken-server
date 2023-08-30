using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.IDO.Entities.Es;
using AwakenServer.IDO.ETOs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.IDO
{
    public class PublicOfferingHandler : IDistributedEventHandler<EntityUpdatedEto<PublicOfferingEto>>,
        IDistributedEventHandler<EntityCreatedEto<PublicOfferingEto>>, ITransientDependency
    {
        private readonly INESTRepository<PublicOffering, Guid> _publicOfferingRepository;

        public PublicOfferingHandler(INESTRepository<PublicOffering, Guid> publicOfferingRepository)
        {
            _publicOfferingRepository = publicOfferingRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<PublicOfferingEto> eventData)
        {
            await _publicOfferingRepository.UpdateAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityCreatedEto<PublicOfferingEto> eventData)
        {
            await _publicOfferingRepository.AddAsync(eventData.Entity);
        }
    }
}