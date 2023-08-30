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
    public class UserPublicOfferingHandler : IDistributedEventHandler<EntityUpdatedEto<UserPublicOfferingEto>>,
        IDistributedEventHandler<EntityCreatedEto<UserPublicOfferingEto>>, ITransientDependency
    {
        private readonly INESTRepository<UserPublicOffering, Guid> _publicOfferingRepository;

        public UserPublicOfferingHandler(INESTRepository<UserPublicOffering, Guid> publicOfferingRepository)
        {
            _publicOfferingRepository = publicOfferingRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<UserPublicOfferingEto> eventData)
        {
            await _publicOfferingRepository.UpdateAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityCreatedEto<UserPublicOfferingEto> eventData)
        {
            await _publicOfferingRepository.AddAsync(eventData.Entity);
        }
    }
}