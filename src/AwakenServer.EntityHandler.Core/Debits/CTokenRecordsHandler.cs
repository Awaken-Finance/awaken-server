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
    public class CTokenRecordsHandler : IDistributedEventHandler<EntityCreatedEto<CTokenRecordChangedEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<CTokenRecord, Guid> _recordRepository;

        public CTokenRecordsHandler(INESTRepository<CTokenRecord, Guid> recordRepository)
        {
            _recordRepository = recordRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<CTokenRecordChangedEto> eventData)
        {
            CTokenRecord esRecord = eventData.Entity;
            await _recordRepository.AddAsync(esRecord);
        }
    }
}