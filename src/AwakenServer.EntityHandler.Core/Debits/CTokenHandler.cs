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
    public class CTokenHandler : IDistributedEventHandler<EntityUpdatedEto<CTokenChangedEto>>,
        IDistributedEventHandler<EntityCreatedEto<CTokenChangedEto>>, ITransientDependency
    {
        private readonly INESTRepository<CToken, Guid> _cTokenRepository;

        public CTokenHandler(INESTRepository<CToken, Guid> cTokenRepository)
        {
            _cTokenRepository = cTokenRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<CTokenChangedEto> eventData)
        {
            var cToken = eventData.Entity;
            await _cTokenRepository.AddOrUpdateAsync(cToken);
        }

        public async Task HandleEventAsync(EntityCreatedEto<CTokenChangedEto> eventData)
        {
            var cToken = eventData.Entity;
            await _cTokenRepository.AddAsync(cToken);
        }
    }
}