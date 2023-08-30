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
    public class CTokenUserInfoHandler : IDistributedEventHandler<EntityUpdatedEto<CTokenUserInfoChangedEto>>,
        IDistributedEventHandler<EntityCreatedEto<CTokenUserInfoChangedEto>>, ITransientDependency
    {
        private readonly INESTRepository<CTokenUserInfo, Guid> _userInfoRepository;

        public CTokenUserInfoHandler(INESTRepository<CTokenUserInfo, Guid> userInfoRepository)
        {
            _userInfoRepository = userInfoRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CTokenUserInfoChangedEto> eventData)
        {
           await _userInfoRepository.UpdateAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityCreatedEto<CTokenUserInfoChangedEto> eventData)
        {
            await _userInfoRepository.AddAsync(eventData.Entity);
        }
    }
}