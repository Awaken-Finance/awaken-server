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
    public class PublicOfferingRecordHandler : IDistributedEventHandler<EntityCreatedEto<PublicOfferingRecordEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<PublicOfferingRecord, Guid> _publicOfferingRepository;

        public PublicOfferingRecordHandler(INESTRepository<PublicOfferingRecord, Guid> publicOfferingRepository)
        {
            _publicOfferingRepository = publicOfferingRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<PublicOfferingRecordEto> eventData)
        {
            await _publicOfferingRepository.AddAsync(eventData.Entity);
        }
    }
}